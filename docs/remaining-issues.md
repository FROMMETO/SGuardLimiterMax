# SGuardLimiterMax 剩余问题跟踪清单

> 本文件由 2026-07-15 维护后审查生成，记录尚未解决、需在后续迭代中处理的问题。  
> 状态说明：⏸ 未开始 / 🔄 进行中 / ✅ 已完成

---

## 一、文档同步

### ✅ README.md 启动机制描述
- **位置：** `README.md` 第 32 行、第 118 行
- **问题：** 声称使用 `HKCU\...\Run` 自启，实际为 `schtasks.exe`
- **修复动作：** 已改为 Task Scheduler 描述
- **状态：** ✅ 已完成

### ✅ README.md 计时器实现描述
- **位置：** `README.md` 第 103-109 行
- **问题：** 声称使用 `winmm.dll` 的 `timeBeginPeriod`，实际为 `ntdll.dll` 的 `NtSetTimerResolution`
- **修复动作：** 已改为 `ntdll.dll` 描述，并更新档位说明
- **状态：** ✅ 已完成

---

## 二、架构优化

### ⏸ 拆分 MainViewModel
- **位置：** `ViewModels/MainViewModel.cs`（当前 731 行）
- **问题：** 集中管理电源计划、自定义游戏、监控循环、热键、更新检查等，新增功能时交叉修改风险高
- **建议方案：**
  1. 提取 `PowerPlanViewModel`：管理 `AvailablePowerPlans`、`SelectedTargetPlan`、`ActivePowerPlanName`、`RefreshPowerPlansAsync`、`ApplyPowerPlan`
  2. 提取 `CustomGamesViewModel`：管理 `CustomGames`、`AddCustomGame`、`RemoveCustomGame`
  3. `MainViewModel` 保留监控循环、优化编排、事件聚合与生命周期管理
- **优先级：** 中
- **状态：** ⏸ 未开始

### ⏸ 为 Service 引入接口与依赖注入
- **位置：** `Services/` 目录下所有类
- **问题：** 全静态类设计导致单元测试无法 mock，状态通过静态字段隐式共享
- **建议方案：**
  1. 定义接口：`IProcessOptimizer`、`IPowerManager`、`ITimerResolutionService`、`IConfigManager` 等
  2. 将现有静态类作为默认实现，新增实例方法包装原有静态方法
  3. `MainViewModel` 通过构造函数接收接口实例
  4. 测试项目中注入 mock 实现
- **优先级：** 中
- **状态：** ⏸ 未开始

---

## 三、测试覆盖

### ⏸ 增加 ProcessOptimizer 测试
- **位置：** `Services/ProcessOptimizer.cs`
- **问题：** 当前测试未覆盖进程优先级、CPU 亲和性计算和游戏进程检测逻辑
- **建议方案：**
  - 先完成 Service 接口化
  - 抽象 `IProcessService` 接口，mock 进程对象
  - 验证 `LastCoreAffinity`、`GetRunningGames`、`ThrottleSGuard`、`ApplyGameSettings` 的决策逻辑
- **优先级：** 中
- **状态：** ⏸ 未开始

### ⏸ 增加异常路径测试
- **位置：** `SGuardLimiterMax.Tests/`
- **问题：** 当前测试以正常路径为主，可补充更多边界情况
- **建议补充：**
  - `ConfigManager`：路径为空、JSON 部分有效、并发写入
  - `PowerManager`：无 active plan 的输出、多语言电源计划名称
  - `GlobalHotkeyService`：大小写混合、重复 modifier、特殊键
- **优先级：** 低
- **状态：** ⏸ 未开始

---

## 四、工程化提升

### ⏸ 添加本地提交前验证钩子
- **位置：** `.git/hooks/` 或 Husky.NET
- **问题：** 目前依赖 CI 发现编译或测试失败，本地缺少强制门禁
- **建议方案：**
  - 添加 pre-push hook，运行 `dotnet build` 和 `dotnet test`
  - 或引入 Husky.NET 进行跨平台管理
- **优先级：** 低
- **状态：** ⏸ 未开始

### ⏸ CI 增加代码格式检查
- **位置：** `.github/workflows/release.yml`
- **问题：** 目前 CI 只验证编译和测试，未验证代码格式
- **建议方案：**
  - 添加 `dotnet format --verify-no-changes` 步骤
  - 或统一 `.editorconfig` 并在本地启用格式检查
- **优先级：** 低
- **状态：** ⏸ 未开始

### ⏸ 统一解决方案文件
- **位置：** 仓库根目录
- **问题：** 当前无 `.sln` 文件，`dotnet build` 默认只构建主项目，`dotnet test` 需要显式指定测试项目路径
- **建议方案：**
  - 创建 `SGuardLimiterMax.sln` 包含主项目和测试项目
  - 更新 CI 使用 `dotnet build SGuardLimiterMax.sln`
- **优先级：** 低
- **状态：** ⏸ 未开始

---

## 五、性能与风险

### ⏸ 评估静态类的线程安全
- **位置：** `Services/PowerManager.cs`、`Services/TimerResolutionService.cs` 等
- **问题：** 多个 Service 使用静态字段（如 `_originalGuid`、`_active`），新增后台任务时需评估并发访问
- **建议方案：**
  - 在引入 DI 时，将状态移至实例字段
  - 对必须共享的状态使用 `lock` 或 `SemaphoreSlim`
- **优先级：** 低
- **状态：** ⏸ 未开始

### ⏸ 更新检查错误处理增强
- **位置：** `Services/UpdateChecker.cs`
- **问题：** 当前所有异常被吞掉，fork 用户可能不知道 URL 配置是否正确
- **建议方案：**
  - 保留静默失败的主要行为
  - 增加可选的诊断日志或调试输出
- **优先级：** 低
- **状态：** ⏸ 未开始

---

## 六、推荐处理顺序

1. **高优先级（下次功能迭代前）**
   - 拆分 `MainViewModel`
   - 为 Service 引入接口与 DI

2. **中优先级（2-3 个迭代内）**
   - 增加 `ProcessOptimizer` 测试
   - 创建 `.sln` 文件

3. **低优先级（有余力时）**
   - 添加 pre-push hook
   - CI 增加格式检查
   - 补充异常路径测试

---

*最后更新：2026-07-15*
