"""Build reproducible native assets before Godot imports the project."""
import os,shutil,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
for folder in ['fonts','audio','data','docs','build']:(ROOT/folder).mkdir(exist_ok=True)
font=Path(os.environ.get('COLOR_WORLD_FONT','/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'))
license=Path('/usr/share/doc/fonts-dejavu-core/copyright')
if not (ROOT/'fonts/Interface.ttf').exists():
 if not font.is_file():raise SystemExit('Set COLOR_WORLD_FONT to DejaVuSans.ttf, or install fonts-dejavu-core.')
 shutil.copy2(font,ROOT/'fonts/Interface.ttf')
 if license.is_file():shutil.copy2(license,ROOT/'fonts/LICENSE.txt')
for script in ['generate_content.py','generate_audio.py','validate_content.py']:
 subprocess.run([sys.executable,str(ROOT/'tools'/script)],check=True)
