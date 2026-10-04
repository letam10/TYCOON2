from pathlib import Path
import json
import re
import tarfile

ROOT = Path(__file__).resolve().parents[1]
TEMPLATE = Path(r'C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Data\Resources\PackageManager\ProjectTemplates\com.unity.template.urp-blank-17.2.1.tgz')

def main():
    with tarfile.open(TEMPLATE, 'r:gz') as archive:
        prefix = 'package/ProjectData~/'
        for member in archive:
            if not member.isfile() or not member.name.startswith(prefix):
                continue
            relative = Path(member.name[len(prefix):])
            if relative.parts[0] not in ('Assets', 'Packages', 'ProjectSettings'):
                continue
            if 'Readme' in relative.name or 'TutorialInfo' in relative.parts:
                continue
            target = (ROOT / relative).resolve()
            if not target.is_relative_to(ROOT) or target.exists():
                continue
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(archive.extractfile(member).read())
    manifest = {'dependencies': {
        'com.unity.ai.navigation': '2.0.12',
        'com.unity.inputsystem': '1.19.0',
        'com.unity.render-pipelines.universal': '17.6.0',
        'com.unity.test-framework': '1.8.0',
        'com.unity.ugui': '2.6.0',
        'com.unity.modules.audio': '1.0.0',
        'com.unity.modules.animation': '1.0.0',
        'com.unity.modules.jsonserialize': '1.0.0',
        'com.unity.modules.physics': '1.0.0',
        'com.unity.modules.ai': '1.0.0',
        'com.unity.modules.imageconversion': '1.0.0',
        'com.unity.modules.screencapture': '1.0.0',
        'com.unity.modules.ui': '1.0.0',
        'com.unity.modules.uielements': '1.0.0'}}
    (ROOT / 'Packages/manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    (ROOT / 'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.6.3f1\nm_EditorVersionWithRevision: 6000.6.3f1 (45d8eee7de74)\n', encoding='utf-8')
    for relative in ['Assets/_Game/Art/Imported', 'Assets/_Game/Art/Materials', 'Assets/_Game/Resources', 'Assets/_Game/Scenes', 'Assets/_Game/Scripts/Runtime', 'Assets/_Game/Scripts/Editor', 'Assets/_Game/Tests/Editor', 'ArtSource/Processed', 'ASSET/Downloaded', 'ASSET/Generated', 'ASSET/Fonts', 'Documentation', 'QA/Evidence', 'work', 'Builds/Windows']:
        (ROOT / relative).mkdir(parents=True, exist_ok=True)
    settings = ROOT / 'ProjectSettings/ProjectSettings.asset'
    text = settings.read_text(encoding='utf-8')
    text = re.sub(r'(?m)^  activeInputHandler:.*$', '  activeInputHandler: 1', text)
    text = re.sub(r'(?m)^  companyName:.*$', '  companyName: TamStudio', text)
    text = re.sub(r'(?m)^  productName:.*$', '  productName: TYCOON2', text)
    settings.write_text(text, encoding='utf-8')
    print(json.dumps({'root': str(ROOT), 'editor': '6000.6.3f1', 'urp': '17.6.0', 'status': 'created'}))

if __name__ == '__main__':
    main()
