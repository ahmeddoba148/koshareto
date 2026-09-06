package com.netdoctor.app;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.net.ConnectivityManager;
import android.net.LinkAddress;
import android.net.LinkProperties;
import android.net.Network;
import android.net.NetworkCapabilities;
import android.net.RouteInfo;
import android.net.wifi.WifiInfo;
import android.net.wifi.WifiManager;
import android.os.Build;
import android.os.IBinder;
import android.os.PowerManager;

import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.Inet4Address;
import java.net.InetAddress;
import java.net.URL;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;
import java.util.concurrent.TimeUnit;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class DiagnosticService extends Service {
    public static final String PREFS = "net_doctor";
    public static final String ACTION_START = "com.netdoctor.app.START";
    public static final String ACTION_STOP = "com.netdoctor.app.STOP";
    private static final String CHANNEL_ID = "net_doctor_monitor";
    private static final int NOTIFICATION_ID = 4811;
    private static final Pattern PING_TIME = Pattern.compile("time[=<]([0-9.]+)\\s*ms", Pattern.CASE_INSENSITIVE);

    private volatile boolean running = false;
    private ExecutorService loopExecutor;
    private ExecutorService probePool;
    private PowerManager.WakeLock wakeLock;
    private final Map<String, TargetStats> stats = new LinkedHashMap<>();
    private final Map<String, Boolean> lastState = new LinkedHashMap<>();

    @Override
    public void onCreate() {
        super.onCreate();
        createNotificationChannel();
        loopExecutor = Executors.newSingleThreadExecutor();
        probePool = Executors.newFixedThreadPool(6);
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        String action = intent == null ? ACTION_START : intent.getAction();
        if (ACTION_STOP.equals(action)) {
            stopDiagnosis("تم إيقاف التشخيص يدويًا");
            return START_NOT_STICKY;
        }

        if (!running) {
            startForeground(NOTIFICATION_ID, buildNotification("جارٍ مراقبة الشبكة تلقائيًا"));
            startDiagnosis();
        }
        return START_NOT_STICKY;
    }

    private void startDiagnosis() {
        running = true;
        SharedPreferences p = getSharedPreferences(PREFS, MODE_PRIVATE);
        p.edit()
                .putBoolean("running", true)
                .putLong("session_start", System.currentTimeMillis())
                .putString("log", "")
                .putString("snapshot", "")
                .apply();
        stats.clear();
        lastState.clear();
        appendLog("=== بدء جلسة Net Doctor ===");

        try {
            PowerManager pm = (PowerManager) getSystemService(POWER_SERVICE);
            wakeLock = pm.newWakeLock(PowerManager.PARTIAL_WAKE_LOCK, "NetDoctor::Monitor");
            wakeLock.acquire(6 * 60 * 60 * 1000L);
        } catch (Exception ignored) { }

        loopExecutor.submit(this::monitorLoop);
    }

    private void monitorLoop() {
        while (running) {
            long cycleStart = System.currentTimeMillis();
            SharedPreferences p = getSharedPreferences(PREFS, MODE_PRIVATE);
            String gateway = detectGateway();
            if (gateway == null || gateway.isEmpty()) gateway = "192.168.1.1";
            String ap = p.getString("ap_ip", "192.168.1.2");
            String dvr = p.getString("dvr_ip", "192.168.1.18");
            String internet = p.getString("internet_ip", "8.8.8.8");

            LinkedHashMap<String, String> targets = new LinkedHashMap<>();
            targets.put("gateway", gateway);
            targets.put("ap", ap);
            targets.put("dvr", dvr);
            targets.put("internet", internet);

            try {
                List<Future<ProbeResult>> futures = new ArrayList<>();
                for (String host : targets.values()) {
                    final String h = host;
                    futures.add(probePool.submit(() -> pingOnce(h)));
                }
                Future<AuxResult> dnsFuture = probePool.submit(this::testDns);
                Future<AuxResult> httpFuture = probePool.submit(this::testHttp);

                LinkedHashMap<String, ProbeResult> results = new LinkedHashMap<>();
                int i = 0;
                for (Map.Entry<String, String> entry : targets.entrySet()) {
                    ProbeResult r;
                    try {
                        r = futures.get(i++).get(2500, TimeUnit.MILLISECONDS);
                    } catch (Exception e) {
                        r = ProbeResult.fail(entry.getValue(), "probe timeout");
                    }
                    results.put(entry.getKey(), r);
                    updateStats(entry.getKey(), entry.getValue(), r);
                    detectStateChange(entry.getKey(), entry.getValue(), r.ok);
                }

                AuxResult dns;
                AuxResult http;
                try { dns = dnsFuture.get(2500, TimeUnit.MILLISECONDS); }
                catch (Exception e) { dns = AuxResult.fail("DNS timeout"); }
                try { http = httpFuture.get(3500, TimeUnit.MILLISECONDS); }
                catch (Exception e) { http = AuxResult.fail("HTTP timeout"); }

                WifiSnapshot wifi = readWifiSnapshot();
                String diagnosis = diagnose(results, dns, http, wifi);
                persistSnapshot(results, dns, http, wifi, diagnosis);
                appendCycleLog(results, dns, http, wifi, diagnosis);

                NotificationManager nm = (NotificationManager) getSystemService(NOTIFICATION_SERVICE);
                if (nm != null) nm.notify(NOTIFICATION_ID, buildNotification(shortDiagnosis(diagnosis)));
            } catch (Exception e) {
                appendLog("خطأ داخلي في دورة الفحص: " + e.getClass().getSimpleName() + " - " + safe(e.getMessage()));
            }

            long elapsed = System.currentTimeMillis() - cycleStart;
            long wait = Math.max(250L, 2000L - elapsed);
            try { Thread.sleep(wait); }
            catch (InterruptedException ignored) { Thread.currentThread().interrupt(); }
        }
    }

    private void updateStats(String key, String host, ProbeResult r) {
        TargetStats s = stats.get(key);
        if (s == null || !host.equals(s.host)) {
            s = new TargetStats(key, host);
            stats.put(key, s);
        }
        s.add(r);
    }

    private void detectStateChange(String key, String host, boolean up) {
        Boolean previous = lastState.put(key, up);
        if (previous == null) {
            appendLog(label(key) + " " + host + " = " + (up ? "ONLINE" : "OFFLINE"));
        } else if (previous != up) {
            appendLog((up ? "✅ عودة الاتصال: " : "❌ بداية انقطاع: ") + label(key) + " " + host);
        }
    }

    private ProbeResult pingOnce(String host) {
        if (host == null || host.trim().isEmpty()) return ProbeResult.fail(host, "empty host");
        Process process = null;
        try {
            long started = System.nanoTime();
            process = new ProcessBuilder("/system/bin/ping", "-c", "1", "-W", "1", host.trim())
                    .redirectErrorStream(true).start();
            StringBuilder output = new StringBuilder();
            try (BufferedReader br = new BufferedReader(new InputStreamReader(process.getInputStream()))) {
                String line;
                while ((line = br.readLine()) != null) output.append(line).append('\n');
            }
            boolean finished = process.waitFor(1600, TimeUnit.MILLISECONDS);
            if (!finished) {
                process.destroyForcibly();
                return ProbeResult.fail(host, "timeout");
            }
            Matcher m = PING_TIME.matcher(output.toString());
            if (process.exitValue() == 0) {
                double ms = (System.nanoTime() - started) / 1_000_000.0;
                if (m.find()) {
                    try { ms = Double.parseDouble(m.group(1)); } catch (Exception ignored) { }
                }
                return ProbeResult.ok(host, ms);
            }
            return ProbeResult.fail(host, "no reply");
        } catch (Exception e) {
            return ProbeResult.fail(host, e.getClass().getSimpleName());
        } finally {
            if (process != null) process.destroy();
        }
    }

    private AuxResult testDns() {
        try {
            long t = System.nanoTime();
            InetAddress a = InetAddress.getByName("www.google.com");
            double ms = (System.nanoTime() - t) / 1_000_000.0;
            return AuxResult.ok(ms, a.getHostAddress());
        } catch (Exception e) {
            return AuxResult.fail(e.getClass().getSimpleName());
        }
    }

    private AuxResult testHttp() {
        HttpURLConnection c = null;
        try {
            long t = System.nanoTime();
            c = (HttpURLConnection) new URL("https://connectivitycheck.gstatic.com/generate_204").openConnection();
            c.setConnectTimeout(2200);
            c.setReadTimeout(2200);
            c.setUseCaches(false);
            c.setRequestMethod("GET");
            int code = c.getResponseCode();
            double ms = (System.nanoTime() - t) / 1_000_000.0;
            boolean ok = code >= 200 && code < 400;
            return ok ? AuxResult.ok(ms, "HTTP " + code) : AuxResult.fail("HTTP " + code);
        } catch (Exception e) {
            return AuxResult.fail(e.getClass().getSimpleName());
        } finally {
            if (c != null) c.disconnect();
        }
    }

    private String detectGateway() {
        try {
            ConnectivityManager cm = (ConnectivityManager) getSystemService(CONNECTIVITY_SERVICE);
            Network n = cm.getActiveNetwork();
            if (n == null) return null;
            LinkProperties lp = cm.getLinkProperties(n);
            if (lp == null) return null;
            for (RouteInfo route : lp.getRoutes()) {
                InetAddress gateway = route.getGateway();
                if (gateway instanceof Inet4Address && route.isDefaultRoute()) return gateway.getHostAddress();
            }
            for (RouteInfo route : lp.getRoutes()) {
                InetAddress gateway = route.getGateway();
                if (gateway instanceof Inet4Address) return gateway.getHostAddress();
            }
        } catch (Exception ignored) { }
        return null;
    }

    private WifiSnapshot readWifiSnapshot() {
        WifiSnapshot s = new WifiSnapshot();
        try {
            ConnectivityManager cm = (ConnectivityManager) getSystemService(CONNECTIVITY_SERVICE);
            Network n = cm.getActiveNetwork();
            if (n != null) {
                NetworkCapabilities nc = cm.getNetworkCapabilities(n);
                s.wifi = nc != null && nc.hasTransport(NetworkCapabilities.TRANSPORT_WIFI);
                LinkProperties lp = cm.getLinkProperties(n);
                if (lp != null) {
                    for (LinkAddress la : lp.getLinkAddresses()) {
                        if (la.getAddress() instanceof Inet4Address) {
                            s.localIp = la.getAddress().getHostAddress();
                            break;
                        }
                    }
                }
            }
            if (s.wifi) {
                WifiManager wm = (WifiManager) getApplicationContext().getSystemService(Context.WIFI_SERVICE);
                WifiInfo wi = wm.getConnectionInfo();
                if (wi != null) {
                    s.rssi = wi.getRssi();
                    s.frequency = wi.getFrequency();
                    s.linkSpeed = wi.getLinkSpeed();
                    s.channel = channelFromFrequency(s.frequency);
                    String ssid = wi.getSSID();
                    if (ssid != null) s.ssid = ssid.replace("\"", "");
                }
            }
        } catch (SecurityException se) {
            s.permissionMissing = true;
        } catch (Exception ignored) { }
        return s;
    }

    private int channelFromFrequency(int f) {
        if (f == 2484) return 14;
        if (f >= 2412 && f <= 2472) return (f - 2407) / 5;
        if (f >= 5000 && f <= 5900) return (f - 5000) / 5;
        if (f >= 5955 && f <= 7115) return (f - 5950) / 5;
        return 0;
    }

    private String diagnose(Map<String, ProbeResult> r, AuxResult dns, AuxResult http, WifiSnapshot wifi) {
        ProbeResult gw = r.get("gateway");
        ProbeResult ap = r.get("ap");
        ProbeResult dvr = r.get("dvr");
        ProbeResult net = r.get("internet");

        if (gw == null || !gw.ok) {
            return "مشكلة نشطة في الشبكة المحلية/الراوتر الأساسي — الهاتف لا يستطيع الوصول إلى الـGateway. الثقة 95%";
        }
        if ((net == null || !net.ok) && !http.ok) {
            return "مشكلة WAN أو مزود الخدمة — الراوتر المحلي يرد لكن الإنترنت الخارجي لا يرد. الثقة 92%";
        }
        if (net != null && net.ok && !dns.ok) {
            return "مشكلة DNS — الوصول للإنترنت بالـIP يعمل لكن حل أسماء المواقع يفشل. الثقة 97%";
        }
        if (ap != null && !ap.ok) {
            return "مشكلة محتملة في الـAccess Point أو الكابل الواصل له — الراوتر الأساسي يعمل والـAP لا يرد. الثقة 88%";
        }
        if (dvr != null && !dvr.ok) {
            return "مشكلة محتملة في الـDVR أو كابل الشبكة الخاص به — باقي الشبكة تعمل والـDVR لا يرد. الثقة 85%";
        }
        if (gw.latencyMs >= 60) {
            if (wifi.wifi && wifi.rssi > -60) {
                return "تذبذب محلي مرتفع رغم قوة إشارة Wi‑Fi الجيدة — الاشتباه الأكبر في Wi‑Fi/استجابة الراوتر. الثقة 78%";
            }
            return "زمن استجابة محلي مرتفع للراوتر — راقب Wi‑Fi والتداخل والمسافة. الثقة 72%";
        }
        if (http.ok && net != null && net.ok && dns.ok) {
            return "لا توجد مشكلة نشطة الآن — الشبكة المحلية والإنترنت وDNS تعمل. استمر في المراقبة حتى لحظة التهنيج.";
        }
        return "الاتصال يعمل جزئيًا ولا توجد علة واحدة مؤكدة في هذه اللحظة — استمر في المراقبة.";
    }

    private void persistSnapshot(Map<String, ProbeResult> results, AuxResult dns, AuxResult http, WifiSnapshot wifi, String diagnosis) {
        try {
            JSONObject root = new JSONObject();
            root.put("time", System.currentTimeMillis());
            for (String key : results.keySet()) {
                TargetStats s = stats.get(key);
                if (s != null) root.put(key, s.toJson());
            }
            root.put("dns", dns.toJson());
            root.put("http", http.toJson());
            root.put("wifi", wifi.toJson());
            root.put("diagnosis", diagnosis);
            getSharedPreferences(PREFS, MODE_PRIVATE).edit()
                    .putString("snapshot", root.toString())
                    .putString("diagnosis", diagnosis)
                    .apply();
        } catch (Exception ignored) { }
    }

    private void appendCycleLog(Map<String, ProbeResult> r, AuxResult dns, AuxResult http, WifiSnapshot wifi, String diagnosis) {
        StringBuilder sb = new StringBuilder("cycle ");
        for (Map.Entry<String, ProbeResult> e : r.entrySet()) {
            ProbeResult pr = e.getValue();
            sb.append(e.getKey()).append('=').append(pr.ok ? fmt(pr.latencyMs) + "ms" : "TIMEOUT").append(' ');
        }
        sb.append("DNS=").append(dns.ok ? fmt(dns.latencyMs) + "ms" : "FAIL").append(' ')
                .append("HTTP=").append(http.ok ? fmt(http.latencyMs) + "ms" : "FAIL");
        if (wifi.wifi) sb.append(" WiFi=").append(wifi.rssi).append("dBm/").append(wifi.frequency).append("MHz/ch").append(wifi.channel);
        appendLog(sb.toString());

        if (diagnosis.startsWith("مشكلة") || diagnosis.startsWith("تذبذب") || diagnosis.startsWith("زمن")) {
            appendLog("⚠ " + diagnosis);
        }
    }

    private void appendLog(String line) {
        SharedPreferences p = getSharedPreferences(PREFS, MODE_PRIVATE);
        String old = p.getString("log", "");
        String next = old + timeNow() + " | " + line + "\n";
        if (next.length() > 300_000) next = next.substring(next.length() - 300_000);
        p.edit().putString("log", next).apply();
    }

    private String timeNow() {
        return new SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSS", Locale.US).format(System.currentTimeMillis());
    }

    private String shortDiagnosis(String d) {
        if (d == null) return "مراقبة الشبكة";
        int idx = d.indexOf('—');
        String s = idx > 0 ? d.substring(0, idx).trim() : d;
        return s.length() > 55 ? s.substring(0, 55) : s;
    }

    private Notification buildNotification(String text) {
        Intent open = new Intent(this, MainActivity.class);
        PendingIntent openPi = PendingIntent.getActivity(this, 1, open,
                PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);
        Intent stop = new Intent(this, DiagnosticService.class).setAction(ACTION_STOP);
        PendingIntent stopPi = PendingIntent.getService(this, 2, stop,
                PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);

        Notification.Builder b = Build.VERSION.SDK_INT >= 26
                ? new Notification.Builder(this, CHANNEL_ID)
                : new Notification.Builder(this);
        return b.setSmallIcon(android.R.drawable.stat_notify_sync)
                .setContentTitle("Net Doctor يعمل")
                .setContentText(text)
                .setContentIntent(openPi)
                .setOngoing(true)
                .addAction(new Notification.Action.Builder(android.R.drawable.ic_media_pause, "إيقاف", stopPi).build())
                .build();
    }

    private void createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= 26) {
            NotificationChannel ch = new NotificationChannel(CHANNEL_ID, "Net Doctor Monitor", NotificationManager.IMPORTANCE_LOW);
            ch.setDescription("مراقبة استقرار الشبكة أثناء جلسة التشخيص");
            NotificationManager nm = (NotificationManager) getSystemService(NOTIFICATION_SERVICE);
            if (nm != null) nm.createNotificationChannel(ch);
        }
    }

    private void stopDiagnosis(String reason) {
        if (running) appendLog(reason);
        running = false;
        getSharedPreferences(PREFS, MODE_PRIVATE).edit().putBoolean("running", false).apply();
        if (wakeLock != null && wakeLock.isHeld()) wakeLock.release();
        stopForeground(STOP_FOREGROUND_REMOVE);
        stopSelf();
    }

    @Override
    public void onDestroy() {
        running = false;
        getSharedPreferences(PREFS, MODE_PRIVATE).edit().putBoolean("running", false).apply();
        if (wakeLock != null && wakeLock.isHeld()) wakeLock.release();
        if (probePool != null) probePool.shutdownNow();
        if (loopExecutor != null) loopExecutor.shutdownNow();
        super.onDestroy();
    }

    @Override
    public IBinder onBind(Intent intent) { return null; }

    private static String label(String key) {
        switch (key) {
            case "gateway": return "الراوتر";
            case "ap": return "Access Point";
            case "dvr": return "DVR";
            case "internet": return "الإنترنت";
            default: return key;
        }
    }

    private static String fmt(double v) { return String.format(Locale.US, "%.1f", v); }
    private static String safe(String s) { return s == null ? "" : s; }

    static class ProbeResult {
        final String host;
        final boolean ok;
        final double latencyMs;
        final String error;
        private ProbeResult(String host, boolean ok, double latencyMs, String error) {
            this.host = host; this.ok = ok; this.latencyMs = latencyMs; this.error = error;
        }
        static ProbeResult ok(String host, double ms) { return new ProbeResult(host, true, ms, ""); }
        static ProbeResult fail(String host, String error) { return new ProbeResult(host, false, -1, error); }
    }

    static class AuxResult {
        final boolean ok;
        final double latencyMs;
        final String detail;
        private AuxResult(boolean ok, double latencyMs, String detail) {
            this.ok = ok; this.latencyMs = latencyMs; this.detail = detail;
        }
        static AuxResult ok(double ms, String detail) { return new AuxResult(true, ms, detail); }
        static AuxResult fail(String detail) { return new AuxResult(false, -1, detail); }
        JSONObject toJson() {
            JSONObject o = new JSONObject();
            try { o.put("ok", ok); o.put("latency", latencyMs); o.put("detail", detail); } catch (Exception ignored) { }
            return o;
        }
    }

    static class TargetStats {
        final String key;
        final String host;
        long sent = 0;
        long success = 0;
        double min = Double.MAX_VALUE;
        double max = 0;
        double sum = 0;
        double lastLatency = -1;
        double jitterSum = 0;
        long jitterCount = 0;
        boolean lastUp = false;
        int consecutiveFails = 0;

        TargetStats(String key, String host) { this.key = key; this.host = host; }

        void add(ProbeResult r) {
            sent++;
            lastUp = r.ok;
            if (r.ok) {
                success++;
                min = Math.min(min, r.latencyMs);
                max = Math.max(max, r.latencyMs);
                sum += r.latencyMs;
                if (lastLatency >= 0) { jitterSum += Math.abs(r.latencyMs - lastLatency); jitterCount++; }
                lastLatency = r.latencyMs;
                consecutiveFails = 0;
            } else {
                consecutiveFails++;
            }
        }

        JSONObject toJson() {
            JSONObject o = new JSONObject();
            try {
                double loss = sent == 0 ? 0 : ((sent - success) * 100.0 / sent);
                double avg = success == 0 ? -1 : sum / success;
                double jitter = jitterCount == 0 ? 0 : jitterSum / jitterCount;
                o.put("label", label(key));
                o.put("host", host);
                o.put("sent", sent);
                o.put("success", success);
                o.put("loss", loss);
                o.put("min", success == 0 ? -1 : min);
                o.put("avg", avg);
                o.put("max", success == 0 ? -1 : max);
                o.put("jitter", jitter);
                o.put("lastLatency", lastLatency);
                o.put("lastUp", lastUp);
                o.put("consecutiveFails", consecutiveFails);
            } catch (Exception ignored) { }
            return o;
        }
    }

    static class WifiSnapshot {
        boolean wifi = false;
        boolean permissionMissing = false;
        String localIp = "";
        String ssid = "";
        int rssi = -127;
        int frequency = 0;
        int channel = 0;
        int linkSpeed = 0;
        JSONObject toJson() {
            JSONObject o = new JSONObject();
            try {
                o.put("wifi", wifi);
                o.put("permissionMissing", permissionMissing);
                o.put("localIp", localIp);
                o.put("ssid", ssid);
                o.put("rssi", rssi);
                o.put("frequency", frequency);
                o.put("channel", channel);
                o.put("linkSpeed", linkSpeed);
            } catch (Exception ignored) { }
            return o;
        }
    }
}
