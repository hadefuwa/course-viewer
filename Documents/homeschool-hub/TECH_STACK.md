# Homeschool Hub - Technology Stack

## Overview

The Homeschool Hub is a Windows desktop application built with modern Microsoft technologies, designed to provide a local, privacy-focused learning platform for children.

## Core Technologies

### 1. **WinUI 3** (Windows UI Library 3)
- **What it is**: Microsoft's modern native UI framework for Windows desktop applications
- **Why we use it**: 
  - Provides a native Windows experience with Fluent Design System
  - Modern, responsive UI controls optimized for Windows 10/11
  - No web browser dependency - true native app performance
  - Access to Windows-specific features and APIs
- **Version**: 1.5.240627000 (via Windows App SDK)

### 2. **.NET 8.0**
- **What it is**: Microsoft's cross-platform development framework
- **Why we use it**:
  - High performance and modern C# language features
  - Excellent Windows desktop application support
  - Strong type safety and memory management
  - Rich standard library
- **Target Framework**: `net8.0-windows10.0.19041.0`

### 3. **C# Programming Language**
- **What it is**: Modern, object-oriented programming language
- **Why we use it**:
  - Type-safe and memory-safe
  - Excellent tooling support (Visual Studio, IntelliSense)
  - Strong async/await support for responsive UI
  - Great for Windows development

### 4. **SQLite** (via Microsoft.Data.Sqlite)
- **What it is**: Lightweight, self-contained database engine
- **Why we use it**:
  - **Local storage**: All data stays on the user's PC - no cloud required
  - **Zero configuration**: No database server needed
  - **Fast and reliable**: Perfect for single-user applications
  - **File-based**: Database stored as a single file in `%LocalAppData%\HomeschoolHub\`
- **Version**: 8.0.0
- **Data stored**:
  - Student profiles
  - Lessons and content
  - Quiz questions and answers
  - Progress tracking
  - Video resources

### 5. **WebView2** (Microsoft Edge WebView2)
- **What it is**: Control that embeds web content using Microsoft Edge (Chromium) rendering engine
- **Why we use it**:
  - Embed YouTube videos directly in the app
  - Display web-based tutorials without leaving the application
  - Modern web standards support
  - Secure sandboxed environment
- **Version**: 1.0.2792.45
- **Usage**: Embedded YouTube videos for Fusion 360 tutorials

## Architecture

### **MVVM-like Pattern**
- **Models**: Data structures (Student, Lesson, Quiz, Progress, VideoResource)
- **Views**: XAML UI pages (MainPage, LessonViewPage, QuizPage, etc.)
- **Data Layer**: DatabaseContext handles all database operations

### **Local-First Architecture**
- All data stored locally in SQLite database
- No internet required for core functionality
- YouTube videos require internet (via WebView2)
- Complete privacy - data never leaves the PC

## Project Structure

```
HomeschoolHub/
├── Models/              # Data models (Student, Lesson, Quiz, etc.)
├── Views/               # UI pages (XAML + code-behind)
├── Data/                # Database layer (SQLite operations)
├── App.xaml             # Application entry point
├── MainWindow.xaml      # Main application window
└── HomeschoolHub.csproj # Project configuration
```

## Key Features Enabled by Tech Stack

### **WinUI 3 Enables**:
- Modern, child-friendly UI with colorful cards and buttons
- Smooth animations and transitions
- Responsive layouts that work on different screen sizes
- Native Windows look and feel

### **SQLite Enables**:
- Fast local data access
- Persistent storage across app sessions
- Progress tracking without external services
- Privacy - all data local

### **WebView2 Enables**:
- Embedded YouTube video playback
- No need to open external browser
- Seamless integration with app UI

## Development Tools

- **Visual Studio 2022**: Recommended IDE with excellent WinUI 3 support
- **.NET SDK 8.0**: Required for building
- **Windows 10/11**: Target platform

## Dependencies

```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.5.240627000" />
<PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.1" />
<PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" />
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2792.45" />
```

## Data Storage Location

- **Database**: `%LocalAppData%\HomeschoolHub\homeschool.db`
- **Platform**: Windows only (native desktop app)
- **Portability**: Database file can be backed up/copied

## Why This Stack?

1. **Privacy**: Everything local - no cloud, no tracking
2. **Performance**: Native Windows app - fast and responsive
3. **Offline**: Works without internet (except YouTube videos)
4. **Modern**: Uses latest Microsoft UI framework
5. **Maintainable**: Clean C# code, well-structured
6. **Extensible**: Easy to add new lessons, quizzes, or features

## Future Possibilities

With this stack, you could easily add:
- More subjects and content
- Custom quiz creation
- Progress reports/export
- Parent dashboard
- Offline video caching
- Print functionality
- Export to PDF

