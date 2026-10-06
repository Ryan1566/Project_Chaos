---
uid: c1a20001
id: project-chaos.config-pipeline.table-repo
parent: project-chaos.config-pipeline
name: {zh: "Excel 源表仓", en: "Excel Table Repo"}
description:
  zh: >
      Excel 源表仓 Assets/Excel/Chaos_excel：只扫本目录（不递归子目录）、只处理 .xlsx、只读第 1 个页签。表名第一个 _ 前的英文段即类名/Json 名，而且不查重—— A_B.xlsx 与 A_C.xlsx 会互相覆盖。
  en: >
      Excel source-table repo at Assets/Excel/Chaos_excel: scanned non-recursively, .xlsx only, first worksheet only. The class and JSON name is the file name up to the first underscore and is not de-duplicated, so A_B.xlsx and A_C.xlsx overwrite each other.
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:10:00Z"
fingerprint: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
source: []
---
