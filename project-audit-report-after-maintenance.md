# SGuardLimiterMax 项目审查报告（维护后）

**审查日期：** 2026-07-15  
**审查目标：** 评估维护后的项目状态，确认是否已具备增量开发条件  
**审查范围：** 完整源码、构建配置、测试覆盖、CI/CD、文档与工程化实践

---

## 执行摘要

| 维度 | 维护前 | 维护后 | 一句话评价 |
|---|---|---|---|
| Architecture（架构） | 7.5 / 10 | **7.5 / 10** | MVVM 分层清晰，静态类设计未改，MainViewModel 仍偏大。 |
| Code Quality（代码质量） | 6.5 / 10 | **7.5 / 10** | 重复方法已删除，日志增加保护，但 README 存在与实现不符的描述。 |
| Engineering（工程化） | 6.0 / 10 | **7.5 / 10** | 已补齐测试、CLAUDE.md 和 CI 测试门禁，但缺少提交前验证钩子。 |
| Perf & Risk（性能与风险） | 7.5 / 10 | **8.0 / 10** | 安全边界明确，诊断日志 capped，但仍需修正 README 中的过时机制描述。 |
| **总分** | **6.9 / 10** | **7.6 / 10** | 维护目标已达成，项目进入可编译、可测试、可协作的增量开发状态。 |

**结论：** 本次维护已修复审查报告中的阻断项（重复 `DiagLog`）和大部分结构性问题（测试、AI 指引、CI 门禁、日志保护）。剩余问题均为非阻断的文档同步与架构优化，可在后续功能迭代中逐步处理。

---

## 一、维护项完成情况

### 1.1 立即执行项（阻断项）

| 原问题 | 状态 | 说明 |
|---|---|---|
| `Services/PowerManager.cs` 重复 `DiagLog` 导致编译失败 | ✅ 已修复 | 删除重复定义，`dotnet build` 通过 |
| 缺少 `CLAUDE.md` / agent 指引 | ✅ 已创建 | 包含架构、构建命令、UI 保留契约、不可提交文件 |

### 1.2 短期补齐项

| 原问题 | 状态 | 说明 |
|---|---|---|
| 无测试项目 | ✅ 已补齐 | 新增 `SGuardLimiterMax.Tests`，24 个用例全部通过 |
| `power_diag.log` 无限增长 | ✅ 已保护 | 超过 1MB 单份轮转 |
| `SECURITY.md` 自启机制描述过时 | ✅ 已修正 | 改为 `schtasks.exe` |
| `UpdateChecker.ApiUrl` 硬编码 | ✅ 已可配置 | 改为 public static 属性 |
| CI 无 `dotnet test` | ✅ 已增加 | `restore → build → test → publish` 流程 |

### 1.3 中期优化项（本次未做，已记录）

| 原问题 | 状态 | 说明 |
|---|---|---|
| `MainViewModel.cs` 职责过重 | ⏸ 未处理 | 仍为 731 行，建议后续拆分 |
| Service 静态类缺乏 DI | ⏸ 未处理 | 不影响当前维护目标 |

---

## 二、详细发现项

### [INCR] README.md:32 / README.md:118 — 启动机制描述仍过时

**位置：** `README.md` 第 32 行、第 118 行  
**严重级别：** Incremental

**问题描述：**
`README.md` 仍声称应用通过 `HKCU\...\Run` 注册表键实现开机自启，但实际上 `Services/StartupManager.cs` 已改为使用 `schtasks.exe` 创建 Task Scheduler 任务。这与已修复的 `SECURITY.md` 形成新的文档不一致。

**修复建议：**
将 `README.md` 中相关描述统一改为 Task Scheduler：
```markdown
注册至 Windows Task Scheduler（`schtasks.exe`），以 `--autostart` 模式静默启动（不显示主界面）。
```

---

### [INCR] README.md:103-109 — 计时器精度实现描述与代码不符

**位置：** `README.md` 第 103-109 行  
**严重级别：** Incremental

**问题描述：**
`README.md` 声称 `TimerResolutionService` 使用 `winmm.dll` 的 `timeBeginPeriod` / `timeEndPeriod`，但实际代码（`Services/TimerResolutionService.cs`）使用的是 `ntdll.dll` 的 `NtSetTimerResolution`。这是维护前已存在的文档漂移。

**修复建议：**
更新 README 中的技术原理描述，改为 `ntdll.dll` / `NtSetTimerResolution`，并说明支持 sub-millisecond（0.5ms）精度。

---

### [STRUCT] ViewModels/MainViewModel.cs — 仍为 731 行，职责过重

**位置：** `ViewModels/MainViewModel.cs`  
**严重级别：** Structural

**问题描述：**
维护未拆分 `MainViewModel`，它仍集中管理电源计划、自定义游戏、监控循环、热键、更新检查等逻辑。新增功能时交叉修改风险仍然存在。

**修复建议：**
在下一个功能迭代前：
1. 提取 `PowerPlanViewModel`（`AvailablePowerPlans`、`SelectedTargetPlan`、`ActivePowerPlanName` 等）
2. 提取 `CustomGamesViewModel`（`CustomGames`、`AddCustomGame`、`RemoveCustomGame`）
3. `MainViewModel` 保留监控循环与生命周期编排，通过组合持有子 ViewModel

---

### [INCR] Service 全静态类设计未改变

**位置：** `Services/` 目录下所有类  
**严重级别：** Incremental

**问题描述：**
所有 Service 仍为 `static class`，单元测试无法 mock 真实系统调用（如 `powercfg`、进程优先级调整）。本次维护通过 `internal static` 解析/序列化方法缓解了部分测试问题，但核心系统调用仍无法被 mock。

**修复建议：**
在第二个重大功能迭代前，为关键服务定义接口（如 `IPowerManager`、`IProcessOptimizer`、`ITimerResolutionService`），并让 `MainViewModel` 通过构造函数接收接口实例。

---

### [INCR] 缺少提交前验证钩子（pre-commit / pre-push）

**位置：** 仓库根目录  
**严重级别：** Incremental

**问题描述：**
虽然 CI 已增加 `dotnet test`，但本地缺少 pre-commit 或 pre-push 钩子。开发者仍可能提交无法编译或测试失败的代码，依赖 CI 才发现问题。

**修复建议：**
可选添加 Husky.NET 或简单的 Git hooks，在 push 前自动运行 `dotnet build` 和 `dotnet test`。

---

### [INCR] 测试未覆盖 `ProcessOptimizer`

**位置：** `SGuardLimiterMax.Tests/`  
**严重级别：** Incremental

**问题描述：**
测试覆盖了 `ConfigManager`、`PowerManager` 解析、`TimerResolutionService` 状态和 `GlobalHotkeyService`，但未覆盖 `ProcessOptimizer` 的进程优先级/亲和性逻辑。该逻辑需要管理员权限和真实进程，难以在 CI 中测试。

**修复建议：**
如果后续引入接口化设计，可为 `ProcessOptimizer` 抽象 `IProcessService` 接口，单元测试使用 mock 进程验证调度决策；真实系统调用保留为集成测试或手动测试。

---

## 三、验证结果

### 本地构建

```powershell
dotnet build --nologo
```

**结果：** 已成功生成，0 个警告，0 个错误。

### 单元测试

```powershell
dotnet test SGuardLimiterMax.Tests/SGuardLimiterMax.Tests.csproj --no-build --nologo
```

**结果：** 失败 0，通过 24，跳过 0，总计 24。

### CI 工作流

`.github/workflows/release.yml` 已更新为：
1. `dotnet restore`
2. `dotnet build --no-restore`
3. `dotnet test --no-build`
4. `dotnet publish`（Standalone / Framework）
5. 生成 SHA256SUMS.txt 并上传产物

---

## 四、值得肯定的方面

| 方面 | 说明 |
|---|---|
| **编译阻断已解除** | `PowerManager.cs` 重复方法删除后，`dotnet build` 可直接通过。 |
| **测试项目建立** | 新增 4 个测试类、24 个用例，优先覆盖纯逻辑路径，避免管理员权限依赖。 |
| **CI 测试门禁** | 发布前必须先通过 `dotnet test`，降低回归风险。 |
| **AI 协作指引** | `CLAUDE.md` 记录了 UI 保留契约和不可提交文件，减少后续 agent 犯错概率。 |
| **日志风险控制** | `power_diag.log` 1MB 单份轮转，避免长期运行无限膨胀。 |
| **更新 URL 可配置** | `UpdateChecker.ApiUrl` 改为可读写属性，支持 fork 独立发布。 |

---

## 五、增量开发适配建议

### 5.1 短期（下次提交前）

1. **同步 `README.md` 的启动机制和计时器实现描述** 与代码保持一致。
2. **确认 `build.ps1` 在本地可正常产出** standalone 与 framework 两个 exe。

### 5.2 中期（进入第 2-3 个功能迭代前）

1. **拆分 `MainViewModel`**：提取 `PowerPlanViewModel` 和 `CustomGamesViewModel`。
2. **为 Service 引入接口与依赖注入**，提升可测试性。
3. **增加 `ProcessOptimizer` 的抽象单元测试**（需先接口化）。

### 5.3 可选工程化提升

1. 添加本地 pre-push hook，自动运行 `dotnet build` 和 `dotnet test`。
2. 在 CI 中增加 `dotnet format --verify-no-changes` 代码风格检查。

---

## 六、审查结论

维护后的 SGuardLimiterMax 已达到增量开发的基线要求：

- ✅ 可编译（`dotnet build` 通过）
- ✅ 可测试（24 个单元测试通过）
- ✅ CI 有测试门禁
- ✅ 有 AI 协作指引文档
- ✅ 阻断性 bug 已修复

剩余未解决问题均为**非阻断性**的文档同步与架构优化，不影响当前进入功能迭代。建议在开始新功能前，先完成 `README.md` 描述同步，并在后续迭代中按计划拆分 `MainViewModel` 和引入 Service 接口。

---

*报告由 Waza /check Project Audit Mode 生成*
