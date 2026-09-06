package com.netdoctor.app;

import android.Manifest;
import android.app.Activity;
import android.app.AlertDialog;
import android.content.ClipData;
import android.content.ClipboardManager;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;
import android.widget.Toast;

import androidx.core.content.FileProvider;

import org.json.JSONObject;

import java.io.File;
import java.io.FileOutputStream;
import java.nio.charset.StandardCharsets;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Locale;

public class MainActivity extends Activity {
    private SharedPreferences prefs;
    private final Handler handler = new Handler(Looper.getMainLooper());

    private TextView statusText;
    private TextView wifiText;
    private TextView gatewayText;
    private TextView apText;
    private TextView dvrText;
    private TextView internetText;
    private TextView diagnosisText;
    private TextView timelineText;
    private Button startButton;
    private Button stopButton;

    private final Runnable refresher = new Runnable() {
        @Override public void run() {
            refreshUi();
            handler.postDelayed(this, 1000);
        }
    };

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        prefs = getSharedPreferences(DiagnosticService.PREFS, MODE_PRIVATE);
        ensureDefaults();
        buildUi();
        requestNeededPermissions();
    }

    @Override
    protected void onResume() {
        super.onResume();
        handler.removeCallbacks(refresher);
        handler.post(refresher);
    }

    @Override
    protected void onPause() {
        handler.removeCallbacks(refresher);
        super.onPause();
    }

    private void ensureDefaults() {
        SharedPreferences.Editor e = prefs.edit();
        if (!prefs.contains("ap_ip")) e.putString("ap_ip", "192.168.1.2");
        if (!prefs.contains("dvr_ip")) e.putString("dvr_ip", "192.168.1.18");
        if (!prefs.contains("internet_ip")) e.putString("internet_ip", "8.8.8.8");
        e.apply();
    }

    private void buildUi() {
        getWindow().setStatusBarColor(Color.rgb(15, 23, 42));

        ScrollView scroll = new ScrollView(this);
        scroll.setFillViewport(true);
        scroll.setBackgroundColor(Color.rgb(244, 247, 251));

        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setPadding(dp(16), dp(18), dp(16), dp(28));
        root.setLayoutDirection(View.LAYOUT_DIRECTION_RTL);
        scroll.addView(root, new ScrollView.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT));

        TextView title = new TextView(this);
        title.setText("Net Doctor");
        title.setTextSize(30);
        title.setTypeface(Typeface.DEFAULT, Typeface.BOLD);
        title.setTextColor(Color.rgb(15, 23, 42));
        title.setGravity(Gravity.START);
        root.addView(title);

        TextView subtitle = new TextView(this);
        subtitle.setText("تشخيص تلقائي لتهنيج الراوتر والشبكة — اضغط ابدأ وسيب التطبيق يراقب كل حاجة لوحده");
        subtitle.setTextSize(14);
        subtitle.setTextColor(Color.rgb(71, 85, 105));
        subtitle.setPadding(0, dp(4), 0, dp(14));
        root.addView(subtitle);

        statusText = card(root, "الحالة الآن");
        wifiText = card(root, "معلومات الاتصال");
        gatewayText = card(root, "الراوتر الأساسي");
        apText = card(root, "Access Point");
        dvrText = card(root, "DVR / الكاميرات");
        internetText = card(root, "الإنترنت");

        LinearLayout actions = new LinearLayout(this);
        actions.setOrientation(LinearLayout.HORIZONTAL);
        actions.setGravity(Gravity.CENTER);
        actions.setPadding(0, dp(4), 0, dp(8));
        root.addView(actions, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT));

        startButton = actionButton("ابدأ التشخيص");
        stopButton = actionButton("إيقاف");
        Button settingsButton = actionButton("الإعدادات");
        actions.addView(startButton, weighted());
        actions.addView(stopButton, weighted());
        actions.addView(settingsButton, weighted());

        diagnosisText = card(root, "النتيجة النهائية / الاشتباه الحالي");
        timelineText = card(root, "آخر الأحداث");
        timelineText.setTextIsSelectable(true);
        timelineText.setTextSize(12);

        LinearLayout logActions = new LinearLayout(this);
        logActions.setOrientation(LinearLayout.HORIZONTAL);
        logActions.setPadding(0, dp(4), 0, 0);
        root.addView(logActions, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT));
        Button copyButton = actionButton("نسخ اللوج بالكامل");
        Button shareButton = actionButton("مشاركة TXT");
        logActions.addView(copyButton, weighted());
        logActions.addView(shareButton, weighted());

        TextView privacy = new TextView(this);
        privacy.setText("كل الفحص واللوج محفوظ محليًا على الموبايل ولا يتم رفعه لأي سيرفر.");
        privacy.setTextSize(12);
        privacy.setTextColor(Color.rgb(100, 116, 139));
        privacy.setGravity(Gravity.CENTER);
        privacy.setPadding(0, dp(14), 0, 0);
        root.addView(privacy);

        startButton.setOnClickListener(v -> startDiagnosis());
        stopButton.setOnClickListener(v -> stopDiagnosis());
        settingsButton.setOnClickListener(v -> showSettings());
        copyButton.setOnClickListener(v -> copyLog());
        shareButton.setOnClickListener(v -> shareLog());

        setContentView(scroll);
    }

    private TextView card(LinearLayout root, String heading) {
        LinearLayout box = new LinearLayout(this);
        box.setOrientation(LinearLayout.VERTICAL);
        box.setPadding(dp(14), dp(12), dp(14), dp(12));
        box.setBackground(rounded(Color.WHITE, 16));
        LinearLayout.LayoutParams bp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        bp.setMargins(0, 0, 0, dp(10));
        root.addView(box, bp);

        TextView h = new TextView(this);
        h.setText(heading);
        h.setTextSize(13);
        h.setTypeface(Typeface.DEFAULT, Typeface.BOLD);
        h.setTextColor(Color.rgb(71, 85, 105));
        box.addView(h);

        TextView body = new TextView(this);
        body.setText("—");
        body.setTextSize(16);
        body.setTextColor(Color.rgb(15, 23, 42));
        body.setPadding(0, dp(5), 0, 0);
        body.setGravity(Gravity.START);
        body.setLineSpacing(0, 1.15f);
        box.addView(body);
        return body;
    }

    private Button actionButton(String text) {
        Button b = new Button(this);
        b.setText(text);
        b.setTextSize(13);
        b.setAllCaps(false);
        b.setTextColor(Color.WHITE);
        b.setBackground(rounded(Color.rgb(30, 64, 175), 12));
        b.setPadding(dp(6), dp(10), dp(6), dp(10));
        return b;
    }

    private LinearLayout.LayoutParams weighted() {
        LinearLayout.LayoutParams p = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f);
        p.setMargins(dp(3), 0, dp(3), 0);
        return p;
    }

    private GradientDrawable rounded(int color, int radiusDp) {
        GradientDrawable d = new GradientDrawable();
        d.setColor(color);
        d.setCornerRadius(dp(radiusDp));
        return d;
    }

    private void startDiagnosis() {
        if (prefs.getBoolean("running", false)) {
            Toast.makeText(this, "التشخيص شغال بالفعل", Toast.LENGTH_SHORT).show();
            return;
        }
        Intent i = new Intent(this, DiagnosticService.class).setAction(DiagnosticService.ACTION_START);
        try {
            if (Build.VERSION.SDK_INT >= 26) startForegroundService(i); else startService(i);
            Toast.makeText(this, "بدأت المراقبة التلقائية", Toast.LENGTH_SHORT).show();
        } catch (Exception e) {
            Toast.makeText(this, "تعذر بدء الفحص: " + e.getClass().getSimpleName(), Toast.LENGTH_LONG).show();
        }
    }

    private void stopDiagnosis() {
        Intent i = new Intent(this, DiagnosticService.class).setAction(DiagnosticService.ACTION_STOP);
        try { startService(i); } catch (Exception ignored) { }
        prefs.edit().putBoolean("running", false).apply();
        refreshUi();
    }

    private void refreshUi() {
        boolean running = prefs.getBoolean("running", false);
        long start = prefs.getLong("session_start", 0);
        String duration = start > 0 ? formatDuration(System.currentTimeMillis() - start) : "00:00:00";
        statusText.setText((running ? "🟢 التشخيص يعمل" : "⚪ التشخيص متوقف") + "\nمدة الجلسة: " + duration);
        startButton.setEnabled(!running);
        stopButton.setEnabled(running);

        String snap = prefs.getString("snapshot", "");
        if (snap == null || snap.isEmpty()) {
            diagnosisText.setText("ابدأ التشخيص وسيظهر هنا السبب الأقرب تلقائيًا عند حدوث أي مشكلة.");
            timelineText.setText(tailLog(prefs.getString("log", ""), 18));
            return;
        }
        try {
            JSONObject root = new JSONObject(snap);
            gatewayText.setText(renderTarget(root.optJSONObject("gateway")));
            apText.setText(renderTarget(root.optJSONObject("ap")));
            dvrText.setText(renderTarget(root.optJSONObject("dvr")));
            internetText.setText(renderTarget(root.optJSONObject("internet")));
            wifiText.setText(renderWifi(root.optJSONObject("wifi"), root.optJSONObject("dns"), root.optJSONObject("http")));
            diagnosisText.setText(root.optString("diagnosis", "—"));
        } catch (Exception e) {
            diagnosisText.setText("تعذر قراءة آخر Snapshot: " + e.getClass().getSimpleName());
        }
        timelineText.setText(tailLog(prefs.getString("log", ""), 18));
    }

    private String renderTarget(JSONObject o) {
        if (o == null) return "لا توجد بيانات بعد";
        boolean up = o.optBoolean("lastUp", false);
        String host = o.optString("host", "—");
        long sent = o.optLong("sent", 0);
        double loss = o.optDouble("loss", 0);
        double last = o.optDouble("lastLatency", -1);
        double min = o.optDouble("min", -1);
        double avg = o.optDouble("avg", -1);
        double max = o.optDouble("max", -1);
        double jitter = o.optDouble("jitter", 0);
        return (up ? "✅ ONLINE" : "❌ OFFLINE") + "   " + host +
                "\nآخر Ping: " + ms(last) + "   |   Loss: " + one(loss) + "%   |   Samples: " + sent +
                "\nMin/Avg/Max: " + ms(min) + " / " + ms(avg) + " / " + ms(max) +
                "   |   Jitter: " + ms(jitter);
    }

    private String renderWifi(JSONObject w, JSONObject dns, JSONObject http) {
        if (w == null) return "لا توجد معلومات اتصال بعد";
        boolean isWifi = w.optBoolean("wifi", false);
        String band = w.optInt("frequency", 0) >= 5000 ? "5 GHz" : (w.optInt("frequency", 0) > 0 ? "2.4 GHz" : "—");
        String ssid = w.optString("ssid", "");
        if (ssid.equals("<unknown ssid>")) ssid = "غير متاح — اسمح بصلاحية Wi‑Fi/الموقع";
        return "الاتصال: " + (isWifi ? "Wi‑Fi" : "غير Wi‑Fi") +
                "\nSSID: " + (ssid.isEmpty() ? "غير متاح" : ssid) + "   |   IP: " + w.optString("localIp", "—") +
                "\nالإشارة: " + w.optInt("rssi", -127) + " dBm   |   " + band + "   |   Channel " + w.optInt("channel", 0) +
                "   |   Link " + w.optInt("linkSpeed", 0) + " Mbps" +
                "\nDNS: " + aux(dns) + "   |   HTTP: " + aux(http);
    }

    private String aux(JSONObject o) {
        if (o == null) return "—";
        return o.optBoolean("ok", false) ? "✅ " + ms(o.optDouble("latency", -1)) : "❌ " + o.optString("detail", "FAIL");
    }

    private void showSettings() {
        LinearLayout box = new LinearLayout(this);
        box.setOrientation(LinearLayout.VERTICAL);
        box.setPadding(dp(20), dp(6), dp(20), 0);
        box.setLayoutDirection(View.LAYOUT_DIRECTION_RTL);

        EditText ap = settingField(box, "IP الـAccess Point", prefs.getString("ap_ip", "192.168.1.2"));
        EditText dvr = settingField(box, "IP الـDVR", prefs.getString("dvr_ip", "192.168.1.18"));
        EditText net = settingField(box, "IP اختبار الإنترنت", prefs.getString("internet_ip", "8.8.8.8"));

        new AlertDialog.Builder(this)
                .setTitle("إعدادات أهداف الفحص")
                .setView(box)
                .setPositiveButton("حفظ", (d, which) -> {
                    prefs.edit()
                            .putString("ap_ip", ap.getText().toString().trim())
                            .putString("dvr_ip", dvr.getText().toString().trim())
                            .putString("internet_ip", net.getText().toString().trim())
                            .apply();
                    Toast.makeText(this, "تم الحفظ", Toast.LENGTH_SHORT).show();
                })
                .setNegativeButton("إلغاء", null)
                .show();
    }

    private EditText settingField(LinearLayout parent, String label, String value) {
        TextView l = new TextView(this);
        l.setText(label);
        l.setTextColor(Color.DKGRAY);
        l.setPadding(0, dp(8), 0, 0);
        parent.addView(l);
        EditText e = new EditText(this);
        e.setSingleLine(true);
        e.setText(value);
        e.setTextDirection(View.TEXT_DIRECTION_LTR);
        e.setGravity(Gravity.START);
        parent.addView(e, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT));
        return e;
    }

    private void copyLog() {
        String log = prefs.getString("log", "");
        if (log == null || log.isEmpty()) {
            Toast.makeText(this, "مفيش لوج لسه", Toast.LENGTH_SHORT).show();
            return;
        }
        ClipboardManager cm = (ClipboardManager) getSystemService(CLIPBOARD_SERVICE);
        cm.setPrimaryClip(ClipData.newPlainText("Net Doctor Log", buildExportText(log)));
        Toast.makeText(this, "تم نسخ اللوج بالكامل — ابعتهولي في الشات", Toast.LENGTH_LONG).show();
    }

    private void shareLog() {
        String log = prefs.getString("log", "");
        if (log == null || log.isEmpty()) {
            Toast.makeText(this, "مفيش لوج لسه", Toast.LENGTH_SHORT).show();
            return;
        }
        try {
            File f = new File(getCacheDir(), "NetDoctor-log-" + System.currentTimeMillis() + ".txt");
            try (FileOutputStream out = new FileOutputStream(f)) {
                out.write(buildExportText(log).getBytes(StandardCharsets.UTF_8));
            }
            Uri uri = FileProvider.getUriForFile(this, getPackageName() + ".fileprovider", f);
            Intent share = new Intent(Intent.ACTION_SEND);
            share.setType("text/plain");
            share.putExtra(Intent.EXTRA_STREAM, uri);
            share.putExtra(Intent.EXTRA_SUBJECT, "Net Doctor diagnostic log");
            share.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            startActivity(Intent.createChooser(share, "مشاركة لوج Net Doctor"));
        } catch (Exception e) {
            Toast.makeText(this, "تعذر إنشاء ملف اللوج: " + e.getClass().getSimpleName(), Toast.LENGTH_LONG).show();
        }
    }

    private String buildExportText(String log) {
        String diagnosis = prefs.getString("diagnosis", "—");
        long start = prefs.getLong("session_start", 0);
        return "NET DOCTOR DIAGNOSTIC LOG\n" +
                "Session start: " + (start > 0 ? new SimpleDateFormat("yyyy-MM-dd HH:mm:ss", Locale.US).format(new Date(start)) : "—") + "\n" +
                "AP: " + prefs.getString("ap_ip", "192.168.1.2") + "\n" +
                "DVR: " + prefs.getString("dvr_ip", "192.168.1.18") + "\n" +
                "Internet target: " + prefs.getString("internet_ip", "8.8.8.8") + "\n" +
                "Current diagnosis: " + diagnosis + "\n" +
                "==================================================\n" + log;
    }

    private void requestNeededPermissions() {
        if (Build.VERSION.SDK_INT < 23) return;
        List<String> missing = new ArrayList<>();
        if (checkSelfPermission(Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED)
            missing.add(Manifest.permission.ACCESS_FINE_LOCATION);
        if (Build.VERSION.SDK_INT >= 33) {
            if (checkSelfPermission(Manifest.permission.NEARBY_WIFI_DEVICES) != PackageManager.PERMISSION_GRANTED)
                missing.add(Manifest.permission.NEARBY_WIFI_DEVICES);
            if (checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED)
                missing.add(Manifest.permission.POST_NOTIFICATIONS);
        }
        if (!missing.isEmpty()) requestPermissions(missing.toArray(new String[0]), 400);
    }

    private String tailLog(String log, int lines) {
        if (log == null || log.trim().isEmpty()) return "لا توجد أحداث بعد";
        String[] all = log.trim().split("\\n");
        int from = Math.max(0, all.length - lines);
        StringBuilder sb = new StringBuilder();
        for (int i = from; i < all.length; i++) sb.append(all[i]).append('\n');
        return sb.toString().trim();
    }

    private String formatDuration(long ms) {
        long s = Math.max(0, ms / 1000);
        return String.format(Locale.US, "%02d:%02d:%02d", s / 3600, (s % 3600) / 60, s % 60);
    }

    private String ms(double v) { return v < 0 ? "—" : one(v) + " ms"; }
    private String one(double v) { return String.format(Locale.US, "%.1f", v); }
    private int dp(int v) { return Math.round(v * getResources().getDisplayMetrics().density); }
}
