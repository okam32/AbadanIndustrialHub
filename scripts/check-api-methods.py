from pathlib import Path
import re

root = Path(__file__).resolve().parent.parent
api = root / 'AbadanHub.Api' / 'Controllers'
allowed = {'Get', 'Post'}
methods = []
for path in sorted(api.glob('*Controller.cs')):
    text = path.read_text(encoding='utf-8')
    for method in re.findall(r'\[Http([A-Za-z]+)(?:\(|\])', text):
        methods.append((path.name, method))
        assert method in allowed, f'Forbidden HTTP verb in {path.name}: Http{method}'
print(f'PASS: {len(methods)} controller HTTP endpoints use only GET and POST')
for file in [root/'AbadanHub.Api'/'AbadanHub.Api.csproj', root/'AbadanHub.Client'/'AbadanHub.Client.csproj']:
    assert 'net8.0' in file.read_text()
print('PASS: API and Blazor projects target .NET 8')
