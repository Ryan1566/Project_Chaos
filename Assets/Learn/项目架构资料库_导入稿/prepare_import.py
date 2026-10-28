"""Prepare Space source documents; no network, Unity calls, or retrieval service.

Run with Python 3.10+: python prepare_import.py
Only writes this preparation directory. Source files are read-only.
"""
from __future__ import annotations

import hashlib
import json
import re
from pathlib import Path

HERE = Path(__file__).resolve().parent
ASSETS = HERE.parents[1]
SOURCE = HERE / '正文'
OUT = HERE / '导入文件'
PAGES = OUT / '独立页面'
DATE = '2026-10-26'
REF = re.compile(r'^@source (.+?)\|(.+)$', re.M)
GROUPS = [
    ('01_底层架构与数据', 1, 6),
    ('02_输入设置与基础框架', 7, 13),
    ('03_地图导航与战斗', 14, 18),
    ('04_玩法与成长系统', 19, 23),
    ('05_编辑器工具与验证', 24, 24),
]
REQUIRED = ['系统定位', '实现逻辑', '具体调用层', '优点', '缺点与边界', '源码导读与学习练习', '可复用轮子']


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def read_code(path: Path) -> tuple[str, str]:
    raw = path.read_bytes()
    for encoding in ('utf-8-sig', 'gb18030'):
        try:
            return raw.decode(encoding), encoding
        except UnicodeDecodeError:
            pass
    raise ValueError(f'Cannot decode source: {path}')


def write(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.rstrip() + '\n', encoding='utf-8', newline='\n')


def main() -> None:
    documents = []
    sources: dict[str, dict] = {}
    extracts = []
    files = sorted(SOURCE.glob('*.md'))
    if len(files) != 24:
        raise ValueError(f'Expected 24 system articles, got {len(files)}')
    for sequence, file in enumerate(files, 1):
        if int(file.name[:2]) != sequence:
            raise ValueError(f'Unexpected article order: {file.name}')
        content = file.read_text(encoding='utf-8')
        headings = re.findall(r'^## (.+)$', content, re.M)
        if headings != REQUIRED:
            raise ValueError(f'Invalid structure in {file.name}: {headings}')
        if len(content) < 700 or content.count('```') % 2:
            raise ValueError(f'Incomplete content or unbalanced fence: {file.name}')
        references = []

        def resolve(match: re.Match) -> str:
            relative, anchor = match.groups()
            path = (ASSETS / relative).resolve()
            if not path.is_relative_to(ASSETS.resolve()):
                raise ValueError(f'Source outside Assets: {relative}')
            code, encoding = read_code(path)
            lines = code.splitlines()
            matches = [i for i, line in enumerate(lines) if anchor in line and not line.lstrip().startswith(('//', '*'))]
            if not matches:
                raise ValueError(f'Missing source anchor: {relative} | {anchor}')
            index = matches[0]
            reference = {'path': 'Assets/' + relative, 'line': index + 1, 'anchor': anchor}
            references.append(reference)
            sources[relative] = {'path': reference['path'], 'sha256': sha(path.read_bytes()),
                                 'encoding': encoding, 'line_count': len(lines)}
            # Bounded source excerpts are evidence aids, not complete method bodies.
            if len(references) <= 2:
                snippet = '\n'.join(lines[index:index + 22])
                extracts.append(f"## {sequence:02d} · {file.stem[3:]} · 摘录{len(references)}\n\n"
                                f"来源：`{reference['path']}:{index + 1}`；核查锚点：`{anchor}`。\n\n"
                                f"以下最多 22 行，可能在函数中途截断，不能代替完整函数。\n\n```{'python' if path.suffix == '.py' else 'csharp'}\n{snippet}\n```\n")
            return f"- `{reference['path']}:{index + 1}` — `{anchor}`"

        rendered = REF.sub(resolve, content)
        if len(references) < 4 or '@source ' in rendered:
            raise ValueError(f'Incomplete references: {file.name}')
        provenance = f'\n> 项目：Project Chaos；核查日期：{DATE}；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。\n'
        first, rest = rendered.split('\n', 1)
        rendered = first + '\n' + provenance + rest
        write(PAGES / file.name, rendered)
        documents.append({'number': sequence, 'title': first.removeprefix('# '),
                          'file': file.name, 'text': rendered, 'references': references,
                          'sha256': sha(rendered.encode('utf-8'))})
    if len(sources) < 75:
        raise ValueError(f'Too few cited source files: {len(sources)}')
    for name, low, high in GROUPS:
        selected = [d for d in documents if low <= d['number'] <= high]
        overview = '# Project Chaos · ' + name[3:] + '\n\n'
        overview += f'源码核查日期：{DATE}。本文由 {len(selected)} 个独立系统篇组成。每篇末节统一为“可复用轮子”。\n\n'
        overview += '\n'.join('- ' + d['title'] for d in selected) + '\n\n---\n\n'
        nested = [re.sub(r'^(#{1,5}) ', r'\1# ', d['text'], flags=re.M) for d in selected]
        write(OUT / (name + '.md'), overview + '\n\n---\n\n'.join(nested))
    evidence = '# Project Chaos · 源码证据摘录\n\n'
    evidence += f'核查日期：{DATE}。这些片段用于配合系统文档定位源码，不是完整项目代码，也不构成运行验证。源码路径和行号属于本次快照，修改后需重新核对。\n\n'
    write(OUT / '06_源码证据摘录.md', evidence + '\n\n'.join(extracts))
    root = (HERE / '资料库首页.md').read_text(encoding='utf-8')
    write(OUT / '00_资料库首页.md', root)
    all_text = root + '\n\n---\n\n' + '\n\n---\n\n'.join(
        re.sub(r'^(#{1,5}) ', r'\1# ', d['text'], flags=re.M) for d in documents)
    write(OUT / 'ProjectChaos_架构学习资料库_完整正文.md', all_text)
    manifest = {'project': 'Project_Chaos', 'reviewed_date': DATE,
                'verification': 'Static source review; no Unity runtime tests in this task',
                'client_import_status': 'pending; local preparation is not client delivery',
                'systems': [{k: v for k, v in d.items() if k != 'text'} for d in documents],
                'sources': sorted(sources.values(), key=lambda s: s['path'])}
    write(HERE / '源码核查清单.json', json.dumps(manifest, ensure_ascii=False, indent=2))
    # Re-read generated artifacts: check no unresolved tokens, damaged encoding or empty outputs.
    outputs = sorted(OUT.rglob('*.md'))
    for path in outputs:
        text = path.read_text(encoding='utf-8')
        if '\ufffd' in text or '@source ' in text or not text.strip():
            raise ValueError(f'Invalid generated document: {path.name}')
    print(json.dumps({'systems': len(documents), 'source_files': len(sources),
                      'references': sum(len(d['references']) for d in documents),
                      'markdown_outputs': len(outputs), 'body_characters': sum(len(d['text']) for d in documents),
                      'client_import_status': 'pending'}, ensure_ascii=False))


if __name__ == '__main__':
    main()
