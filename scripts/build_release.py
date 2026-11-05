"""Build and validate local release ZIPs. Does not modify Git or access the network."""
import hashlib
import json
from pathlib import Path
import subprocess
from zipfile import ZipFile, ZIP_DEFLATED

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = 'com.baxter.material-override-vrm'
TAG = 'v0.3.2'

def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def build():
    manifest = read_json(ROOT / 'Packages' / PACKAGE / 'package.json')
    if manifest['name'] != PACKAGE or manifest['version'] != TAG[1:]:
        raise ValueError('Package identity/version does not match the release')
    tracked = subprocess.check_output(['git', '-C', str(ROOT), 'ls-files', '-z']).decode().split('\0')
    tracked = [p for p in tracked if p]
    output = ROOT / 'dist'
    output.mkdir(exist_ok=True)
    artifacts = []

    def archive(filename, prefix, extra=None):
        paths = {p[len(prefix):]: ROOT / p for p in tracked if p.startswith(prefix)}
        paths.update(extra or {})
        if not paths:
            raise ValueError(f'No tracked files under {prefix}')
        forbidden = {'library', 'temp', 'logs', 'obj', 'usersettings', '.git', '.agents', '.codex'}
        for relative, path in paths.items():
            if path.is_symlink() or any(part.lower() in forbidden for part in Path(relative).parts):
                raise ValueError(f'Unexpected release file: {relative}')
        destination = output / filename
        with ZipFile(destination, 'w', ZIP_DEFLATED) as bundle:
            for relative, path in sorted(paths.items()):
                bundle.write(path, relative)
        with ZipFile(destination) as bundle:
            if bundle.testzip() is not None:
                raise ValueError(f'Invalid ZIP: {destination}')
            for relative, path in paths.items():
                if bundle.read(relative) != path.read_bytes():
                    raise ValueError(f'ZIP content mismatch: {relative}')
        artifacts.append(destination)
        return destination

    package = archive(f'{PACKAGE}-{TAG[1:]}.zip', f'Packages/{PACKAGE}/', {'CHANGELOG.md': ROOT / 'CHANGELOG.md'})
    with ZipFile(package) as bundle:
        assert json.loads(bundle.read('package.json'))['version'] == TAG[1:]
        assert 'LICENSE' in bundle.namelist()
        assert not any(name in manifest.get('dependencies', {}) for name in ('com.vrmc.gltf', 'com.vrmc.vrm'))
        assert manifest['url'].endswith(f'/{PACKAGE}-{TAG[1:]}.zip')
        for folder in ('Runtime', 'Editor'):
            asmdef = json.loads(bundle.read(f'{folder}/MaterialOverridableVrm.{folder}.asmdef'))
            assert set(asmdef['defineConstraints']) == {'MOUV_HAS_UNIGLTF', 'MOUV_HAS_VRM10'}
            assert all(d['expression'] == '0.131.4' for d in asmdef['versionDefines'])
        assert any(p.startswith('Editor/') for p in bundle.namelist())
        assert any(p.startswith('Runtime/') for p in bundle.namelist())
    for template in ('Unity2022', 'Unity6'):
        project = archive(f'MaterialOverrideVrm-{template}-{TAG}.zip', f'Examples/{template}/', {'LICENSE': ROOT / 'LICENSE'})
        with ZipFile(project) as bundle:
            settings = json.loads(bundle.read('Packages/manifest.json'))
            assert settings['dependencies'][PACKAGE].endswith('#' + TAG)
            lock = json.loads(bundle.read('Packages/packages-lock.json'))
            assert PACKAGE not in lock['dependencies'], 'Stale package hash in project ZIP'
            assert 'Assets/Scenes/Export.unity' in bundle.namelist()
            assert 'ProjectSettings/ProjectVersion.txt' in bundle.namelist()
    sums = []
    for artifact in artifacts:
        digest = hashlib.sha256(artifact.read_bytes()).hexdigest()
        sums.append(f'{digest}  {artifact.name}')
        print(f'{artifact.name}: {artifact.stat().st_size:,} bytes; ZIP/content validation passed')
    (output / 'SHA256SUMS.txt').write_text('\n'.join(sums) + '\n', encoding='utf-8')

if __name__ == '__main__':
    build()
