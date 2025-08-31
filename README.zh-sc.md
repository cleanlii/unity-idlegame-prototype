# 放置类小游戏原型Demo

[![English](https://img.shields.io/badge/lang-English-blue.svg)](https://github.com/cleanlii/unity-idlegame-prototype/blob/master/README.md)
[![SC](https://img.shields.io/badge/lang-中文-red.svg)](https://github.com/cleanlii/unity-idlegame-prototype/blob/master/README.zh-sc.md)

基于Unity开发的放置类挂机RPG游戏，玩家通过不同路线爬塔，并通过自动化系统进行战斗、管理、培养角色。

## 核心概念

### 轻松放置
游戏全程自动运行，玩家无需复杂操作或高强度挑战，就能稳定获得成长。

### 类爬塔机制

灵感来源于《杀戮尖塔》和《黑暗地牢》，本原型包含分支路径系统，玩家可以自由选择三条不同路线，并自动推进：

- **战斗路线**：自动进行 1v1 战斗以获取经验和金币  
- **经济路线**：随时间被动产出金币  
- **经验路线**：随时间被动产出经验  

### 异步玩法

- **离线奖励**：离线时仍可持续获得经验与金币  
- **（TODO）碎片化时间**：利用碎片时间完成限时任务并获取额外奖励  
- **（TODO）异步多人**：排行榜、好友异步对战等  

---

## 项目结构

```
Assets/Scripts/
├── Core/                          # 核心游戏系统
│   ├── GameManager.cs            # 主游戏协调器和入口点
│   ├── BattleManager.cs          # 自动战斗系统和战斗逻辑
│   ├── PlayerController.cs      # 玩家在路线间的移动
│   └── Systems/                  # 专业化游戏系统
│       ├── CharacterSystem.cs   # 角色管理和进度系统
│       ├── SpireSystem.cs       # 路线管理和切换逻辑
│       ├── SaveSystem.cs        # 数据持久化和加密
│       ├── IdleLogSystem.cs     # 实时行为日志系统
│       ├── EcoSystem.cs         # （经济功能占位符）
│       └── GachaSystem.cs       # （抽卡机制占位符）
├── Data/                         # ScriptableObject配置文件
│   ├── PlayerData.cs            # 玩家进度和统计数据
│   ├── CharacterConfig.cs       # 角色模板和成长曲线
│   ├── CharacterData.cs         # 运行时角色实例
│   ├── CharacterDatabase.cs     # 角色收藏和抽卡权重
│   ├── EnemyConfig.cs           # 敌人模板
│   ├── EnemyData.cs             # 运行时敌人实例
│   ├── RouteConfig.cs           # 路线奖励配置
│   └── OfflineRewardConfig.cs   # 离线进度设置
├── Utils/                        # 工具和辅助类
│   ├── ServiceLocator.cs        # 依赖注入模式
│   ├── EventManager.cs          # 类型安全事件系统
│   ├── JsonUtils.cs             # 加密JSON序列化
│   ├── IdleGameConst.cs         # 游戏常量和文件路径
│   └── LogEntryUI.cs            # 日志条目UI组件
└── UIManager.cs                  # UI协调和动画管理
```
---

## 开发备注

### **外部插件**
- **DOTween**：基础 UI 与游戏动画  
- **TextMeshPro**：灵活的 UI 文本渲染  
- **ScriptableObject**：数据驱动的配置，方便编辑器调整  

### **工具类**
- **JsonUtils**：加密 JSON 存档/加载  
- **ServiceLocator**：统一的服务注册、获取与生命周期管理  
- **IdleGameConst**：常量与全局配置  
- **LubanConfig**：表格 → 数据流水线，方便规模化内容管理  

### **编辑器工具**
TBD

### **未来计划**
#### 架构层面
- **表格驱动数据**：外部可控数据平衡，更好的后端服务支持  
- **资源管理优化**：热更新支持（Addressable + HybridCLR）  
- **数据分析工具**：接入官方或者第三方数据分析工具，用于统计追踪  

#### 游戏层面
- **角色切换**：允许玩家随时切换、培养任意角色  
- **装备系统**：引入 RPG 式装备成长  
- **背包系统**：统一管理状态、buff、装备、可控角色列表  
- **更多尖塔机制**：地图预览、Boss战、更完整的进度跟踪  
- ……  