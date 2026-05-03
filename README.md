# 目标地图 (Goal Map)

一款个人目标管理桌面工具，帮助你规划、追踪和回顾人生目标。

## 功能特性

### 📋 任务管理
- **多类型任务**：一次性、周期性（每日/工作日/周末/每周/每月/间隔/自定义）、量化任务
- **日期视图**：顶部横向日期条，快速切换查看每天任务
- **任务树结构**：支持主任务和子任务层级展示
- **拖拽排序**：任务可拖拽调整优先级
- **进度追踪**：量化任务支持数值输入，自动计算进度

### 🎯 目标管理
- **目标分类**：短期、长期、灵感三种时间框架
- **标签系统**：自定义标签和颜色，便于分类管理
- **目标层级**：支持父子目标嵌套
- **自动进度**：子任务完成后自动更新父目标进度
- **量化目标**：支持数值型目标追踪

### 📅 日历视图
- **月度日历**：直观展示每日任务分布
- **颜色标记**：不同目标/任务用不同颜色区分
- **详情侧栏**：点击日期查看当日任务详情

### 🗺️ 目标地图
- **树形结构**：可视化展示目标层级关系
- **进度环**：每个目标节点显示完成进度
- **全局概览**：一目了然的目标体系

### 📊 数据看板
- **打卡率**：任务完成天数/预期天数
- **剩余天数**：距离截止日期的天数
- **连续打卡**：连续完成任务的天数
- **日历热力图**：按日展示任务完成状态（绿色完成、红色未完成）

### 📝 定期盘点
- **周/月视图**：按周期查看任务完成情况
- **统计卡片**：完成率、已完成数、待完成数、打卡次数
- **趋势图表**：每日完成数量折线图
- **目标进度**：各目标的完成进度列表

### ⚙️ 设置
- **开机自启**：Windows 开机自动启动
- **音效设置**：任务完成时播放提示音
- **数据备份**：支持单独备份或全部备份
- **数据导入**：从备份文件恢复数据

### ⏱️ 专注计时器
- **计时模式**：正计时/倒计时
- **悬浮窗口**：始终置顶，不影响其他操作
- **会话记录**：保存专注时长和时间段

## 技术栈

- **框架**：.NET 8 WPF
- **架构**：MVVM 模式
- **存储**：JSON 文件本地存储（无数据库依赖）
- **依赖**：纯 .NET 8 内置库，无第三方 NuGet 包

## 安装运行

```bash
# 克隆项目
git clone https://github.com/nailao946/OKR.git


# 构建
dotnet build "ME\ME.csproj"

# 运行
dotnet run --project "ME\ME.csproj"
```

## 项目结构

```
ME/
├── Core/              # 基础类（RelayCommand, ViewModelBase, EventAggregator）
├── Models/            # 数据模型
│   ├── Goal.cs        # 目标模型
│   ├── TaskItem.cs    # 任务模型
│   ├── GoalTag.cs     # 标签模型
│   ├── Vision.cs      # 愿景模型
│   ├── Review.cs      # 盘点模型
│   ├── FocusSession.cs # 专注会话模型
│   └── AppSettings.cs # 应用设置模型
├── Data/              # 数据存储层
│   ├── JsonStore.cs   # JSON 序列化/反序列化
│   └── Repository.cs  # 数据仓库
├── Services/          # 业务逻辑服务
│   ├── GoalService.cs
│   ├── TaskService.cs
│   ├── FocusTimerService.cs
│   ├── BackupService.cs
│   └── SoundService.cs
├── ViewModels/        # MVVM ViewModel
│   ├── MainViewModel.cs
│   ├── TasksViewModel.cs
│   ├── GoalsViewModel.cs
│   ├── CalendarViewModel.cs
│   ├── MapViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── ReviewViewModel.cs
│   └── SettingsViewModel.cs
├── Views/             # XAML 页面
│   ├── MainWindow.xaml
│   ├── TasksView.xaml
│   ├── GoalsView.xaml
│   ├── CalendarView.xaml
│   ├── MapView.xaml
│   ├── DashboardView.xaml
│   ├── ReviewView.xaml
│   └── SettingsView.xaml
├── Dialogs/           # 对话框窗口
│   ├── GoalEditDialog.xaml
│   ├── TaskEditDialog.xaml
│   ├── TagEditDialog.xaml
│   ├── QuantitativeInputDialog.xaml
│   └── FocusTimerWindow.xaml
└── Resources/         # 资源文件
    └── Styles.xaml    # 全局样式（iOS 风格圆角）
```

## 数据存储

数据存储在 `%LOCALAPPDATA%/ME/JsonData/` 目录下：

| 文件 | 内容 |
|------|------|
| `goals.json` | 目标数据 |
| `tasks.json` | 任务数据 |
| `tags.json` | 标签数据 |
| `visions.json` | 愿景数据 |
| `reviews.json` | 盘点记录 |
| `focus_sessions.json` | 专注会话记录 |
| `settings.json` | 应用设置 |

## 设计风格

- **iOS/macOS 风格**：圆角卡片（12px）、柔和阴影、简洁白色背景
- **主色调**：蓝色 (#007AFF)
- **目标颜色**：红、绿、蓝、粉、灰、黄六色分类
- **中文界面**：全中文标签和提示

## 任务类型说明

| 类型 | 说明 |
|------|------|
| 一次性 | 完成后标记完成 |
| 每日 | 每天重置 |
| 工作日 | 周一至周五 |
| 周末 | 周六、周日 |
| 每周 | 指定星期几 |
| 每月 | 指定日期或月末 |
| 间隔 | 每 N 天一次 |
| 自定义 | 每周 N 次，每天 M 次 |
| 量化 | 数值型目标，支持累加/更新模式 |
