# Homeschool Hub

A Windows application for homeschooling that provides learning materials, quizzes, and progress tracking. All data is stored locally on each PC.

## Features

- **Learning Materials**: Interactive lessons for various subjects
  - Reception Age Maths (counting, adding, subtracting, shapes)
  - Arduino IDE lessons (void setup, void loop, programming basics)
  
- **Quizzes & Challenges**: Test knowledge with interactive quizzes
  - Viking quizzes for 7-year-olds
  
- **Video Tutorials**: Embedded YouTube videos
  - Fusion 360 tutorials for 3D design

- **Progress Tracking**: Monitor learning progress locally
  - View completed lessons and quiz scores
  - Track progress over time

- **Local Data Storage**: All data stored securely on each PC
  - SQLite database in LocalAppData folder
  - No cloud storage, complete privacy

## Requirements

- Windows 10 version 1809 (build 17763) or later
- **Windows App Runtime 1.5** (required - see installation below)
- .NET 8.0 SDK (for building from source)
- Visual Studio 2022 (or later) with:
  - Windows App SDK workload
  - .NET desktop development workload

## Installing Windows App Runtime

**IMPORTANT**: Before running the app, you need to install the Windows App Runtime.

### Quick Install (Recommended)
When you first run the app and see the error dialog:
1. Click **"Yes"** - this will open the Microsoft Store
2. Install "Windows App Runtime" from the Store
3. Restart the app

### Manual Install
1. Go to: https://aka.ms/windowsappruntime
2. Download and run the Windows App Runtime installer
3. Restart the app

### Using Command Line
```powershell
winget install Microsoft.WindowsAppRuntime
```

**Note**: This is a one-time installation per PC. Once installed, the app will run normally.

## Building the Application

1. **Install Prerequisites**:
   - Download and install [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
   - Install [Visual Studio 2022](https://visualstudio.microsoft.com/) with the required workloads

2. **Open the Project**:
   - Open `HomeschoolHub.csproj` in Visual Studio
   - Or use the command line: `dotnet restore` then `dotnet build`

3. **Run the Application**:
   - Press F5 in Visual Studio (recommended - handles runtime automatically)
   - Or use: `dotnet run` (requires Windows App Runtime installed first)
   
   **Note**: If you see an error about Windows App Runtime, click "Yes" to install it from the Microsoft Store.

## First Time Setup

1. When you first launch the app, you'll be prompted to create a student profile
2. Enter the student's name and age
3. Click "Create Student" to begin

## Using the Application

### Main Menu
- **Maths**: Access reception-age math lessons
- **Vikings**: Take Viking history quizzes (age 7+)
- **Arduino**: Learn Arduino programming basics
- **Fusion 360**: Watch 3D design tutorials
- **View My Progress**: See completed activities and scores
- **Change Student**: Switch between student profiles

### Taking Lessons
1. Click on a subject card (e.g., Maths, Arduino)
2. Browse available lessons
3. Read the lesson content
4. Click "Mark as Complete" when finished

### Taking Quizzes
1. Click on a subject with quizzes (e.g., Vikings)
2. Select a quiz to take
3. Answer all questions
4. Click "Submit Quiz" to see your score
5. Your score is automatically saved

### Watching Videos
1. Click on Fusion 360
2. Select a video tutorial
3. Watch the embedded YouTube video
4. Videos play directly in the app

### Viewing Progress
1. Click "View My Progress" from the main menu
2. See all completed lessons and quizzes
3. View scores and completion dates

## Data Storage

All data is stored locally in:
```
%LocalAppData%\HomeschoolHub\homeschool.db
```

This includes:
- Student profiles
- Lesson content
- Quiz questions and answers
- Progress records
- Video resources

## Adding Custom Content

You can extend the application by modifying the `DatabaseContext.cs` file's seed methods:
- `SeedMathsLessons()` - Add more math lessons
- `SeedVikingQuizzes()` - Add more history quizzes
- `SeedArduinoLessons()` - Add more programming lessons
- `SeedFusion360Videos()` - Add more video resources

## Technical Details

- **Framework**: WinUI 3
- **Language**: C#
- **Database**: SQLite (Microsoft.Data.Sqlite)
- **Video Player**: WebView2 (for YouTube embedding)

## Troubleshooting

**App won't start:**
- Ensure you have .NET 8.0 SDK installed
- Check that Windows App SDK is installed
- Verify Visual Studio workloads are complete

**Videos won't play:**
- Ensure WebView2 runtime is installed (usually comes with Windows)
- Check internet connection for YouTube videos

**Database errors:**
- Check that the LocalAppData folder is writable
- Try deleting the database file to reset (data will be lost)

## License

This is a personal project for homeschooling use.

