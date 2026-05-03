# AGENTS.md

## Project Overview
WPF桌面应用"目标地图" - 个人目标管理工具，基于.NET 8 WPF + JSON本地存储。

## Build & Run
```bash
# 构建
dotnet build "C:\Users\admin\Desktop\ME\Fangan\ME\ME.csproj"

# 运行
dotnet run --project "C:\Users\admin\Desktop\ME\Fangan\ME\ME.csproj"
```

## Architecture
- **Models/**: Goal, TaskItem, Vision, Review, FocusSession, AppSettings
- **Data/**: JSON文件存储 (JsonStore) - 无数据库依赖
- **Services/**: GoalService, TaskService, VisionService, ReviewService, FocusTimerService, BackupService
- **ViewModels/**: MVVM模式，每个页面对应一个ViewModel
- **Views/**: XAML页面 + code-behind
- **Resources/Styles.xaml**: 全局样式，iOS风格圆角

## Key Design Decisions
- 数据存储: System.Text.Json序列化到 `%LOCALAPPDATA%/ME/JsonData/*.json`
- 无NuGet依赖: 纯.NET 8内置库
- MVVM模式: 但部分页面(code-behind)直接操作Repository以简化
- 颜色系统: 6色目标分类(Red/Green/Blue/Pink/Gray/Yellow)

## Common Issues & Fixes
1. **TextBox无法输入**: InputTextBoxStyle模板必须包含IsFocused触发器设置BorderBrush
2. **Foreground颜色错误**: 颜色(Color)不能直接用作Foreground，必须用SolidColorBrush
3. **View实例化**: MainWindow动态创建View，避免XAML内联DataContext导致的初始化问题
4. **命名空间冲突**: System.Windows.Controls.Border与System.Windows.Forms.Border冲突时需完全限定

## File Structure
```
ME/
├── Core/           # RelayCommand, ViewModelBase, EventAggregator
├── Models/         # 数据模型
├── Data/           # JsonStore + Repository层
├── Services/       # 业务逻辑
├── ViewModels/     # MVVM ViewModel
├── Views/          # XAML页面
└── Resources/      # Styles.xaml主题
```

## Testing
- 无自动化测试框架
- 手动测试: 运行应用后测试各页面功能

## Conventions
- XAML控件: 使用StaticResource引用Styles.xaml中定义的样式
- 按钮样式: PrimaryButtonStyle(蓝色), SecondaryButtonStyle(白色边框)
- 卡片样式: CardStyle(圆角12px, 阴影)
- 输入框: InputTextBoxStyle(圆角8px, 聚焦蓝色边框)
