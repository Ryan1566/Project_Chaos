---
uid: 24f8b7c9
id: project-chaos.project-infra.repo-root
parent: project-chaos.project-infra
name: {zh: "仓库根文件", en: "Repo Root Files"}
description:
  zh: >
      仓库根元数据：README 占位、.gitignore、.vsconfig 与生成的解决方案文件。同目录的 .csproj 是 Unity 自动生成的产物。
      
  en: >
      Repository-root metadata: README stub, .gitignore, .vsconfig and the generated solution file. The .csproj files here are Unity-regenerated artifacts.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:08:07.156Z"
fingerprint: 072d4ae2ab381b18c96f8af1529caba3dd386b4c56bc2b950dbae4d8d4b8276d
source:
  - path: "README.md"
  - path: ".gitignore"
  - path: ".vsconfig"
  - path: "Project_Chaos.sln"
apis:
  - protocol: file
    path: "README.md"
    description:
      zh: >
          仓库 README——目前只有一行标题（15 字节）。
          
      en: >
          Repo README — currently a one-line stub (15 bytes).
          
  - protocol: file
    path: ".gitignore"
    description:
      zh: >
          Git 忽略清单：Library/Logs/Temp/UserSettings 以及 Assets/Excel、Assets/Story*、Assets/Chaos_Story*、Assets/DocsSrc、Assets/Screenshots。
          
      en: >
          Git ignore list: Library/Logs/Temp/UserSettings plus Assets/Excel, Assets/Story*, Assets/Chaos_Story*, Assets/DocsSrc, Assets/Screenshots.
          
  - protocol: file
    path: ".vsconfig"
    description:
      zh: >
          Visual Studio 工作负载提示（ManagedGame）。
          
      en: >
          Visual Studio workload hint (ManagedGame).
          
  - protocol: file
    path: "Project_Chaos.sln"
    description:
      zh: >
          解决方案文件，引用 8 个生成的 csproj（Assembly-CSharp、spine-unity 等）。
          
      en: >
          Solution referencing the 8 generated csproj files (Assembly-CSharp, spine-unity, ...).
          
---
