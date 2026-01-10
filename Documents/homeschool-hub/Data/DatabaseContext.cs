using Microsoft.Data.Sqlite;
using HomeschoolHub.Models;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace HomeschoolHub.Data
{
    public class DatabaseContext
    {
        private readonly string _connectionString;

        public DatabaseContext()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HomeschoolHub", "homeschool.db");
            var dbDirectory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory!);
            }
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Create Students table
            var createStudentsTable = @"
                CREATE TABLE IF NOT EXISTS Students (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Age INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL
                )";

            // Create Lessons table
            var createLessonsTable = @"
                CREATE TABLE IF NOT EXISTS Lessons (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Category TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    AgeGroup INTEGER NOT NULL,
                    OrderIndex INTEGER NOT NULL
                )";

            // Create Quizzes table
            var createQuizzesTable = @"
                CREATE TABLE IF NOT EXISTS Quizzes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Category TEXT NOT NULL,
                    AgeGroup INTEGER NOT NULL
                )";

            // Create Questions table
            var createQuestionsTable = @"
                CREATE TABLE IF NOT EXISTS Questions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    QuizId INTEGER NOT NULL,
                    QuestionText TEXT NOT NULL,
                    Options TEXT NOT NULL,
                    CorrectAnswerIndex INTEGER NOT NULL,
                    FOREIGN KEY (QuizId) REFERENCES Quizzes(Id)
                )";

            // Create Progress table
            var createProgressTable = @"
                CREATE TABLE IF NOT EXISTS Progress (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentId INTEGER NOT NULL,
                    LessonId INTEGER,
                    QuizId INTEGER,
                    Completed INTEGER NOT NULL,
                    Score INTEGER,
                    MaxScore INTEGER,
                    CompletedAt TEXT,
                    FOREIGN KEY (StudentId) REFERENCES Students(Id),
                    FOREIGN KEY (LessonId) REFERENCES Lessons(Id),
                    FOREIGN KEY (QuizId) REFERENCES Quizzes(Id)
                )";

            // Create VideoResources table
            var createVideoResourcesTable = @"
                CREATE TABLE IF NOT EXISTS VideoResources (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Category TEXT NOT NULL,
                    YouTubeVideoId TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    AgeGroup INTEGER NOT NULL
                )";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = createStudentsTable;
                command.ExecuteNonQuery();
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = createLessonsTable;
                command.ExecuteNonQuery();
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = createQuizzesTable;
                command.ExecuteNonQuery();
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = createQuestionsTable;
                command.ExecuteNonQuery();
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = createProgressTable;
                command.ExecuteNonQuery();
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = createVideoResourcesTable;
                command.ExecuteNonQuery();
            }

            // Seed initial data if tables are empty
            SeedInitialData(connection);
        }

        private void SeedInitialData(SqliteConnection connection)
        {
            // Check if data already exists
            using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM Lessons";
            var lessonCount = Convert.ToInt64(checkCommand.ExecuteScalar());

            if (lessonCount > 0) return; // Data already seeded

            // Seed Reception Age Maths lessons
            SeedMathsLessons(connection);
            
            // Seed Viking quizzes
            SeedVikingQuizzes(connection);
            
            // Seed Arduino lessons
            SeedArduinoLessons(connection);
            
            // Seed Fusion 360 videos
            SeedFusion360Videos(connection);
        }

        private void SeedMathsLessons(SqliteConnection connection)
        {
            var lessons = new[]
            {
                ("Counting to 10", "Maths", "Let's learn to count! Count the objects: 🍎🍎🍎🍎🍎\n\nCan you count how many apples there are? Practice counting from 1 to 10!", 4, 1),
                ("Adding Numbers", "Maths", "Adding means putting numbers together!\n\nExample: 2 + 3 = ?\n\nIf you have 2 apples and get 3 more, how many do you have? That's right, 5!", 4, 2),
                ("Subtracting Numbers", "Maths", "Subtracting means taking away!\n\nExample: 5 - 2 = ?\n\nIf you have 5 cookies and eat 2, how many are left? That's right, 3!", 4, 3),
                ("Shapes", "Maths", "Let's learn about shapes!\n\n🔵 Circle - round like a ball\n\n⬜ Square - has 4 equal sides\n\n🔺 Triangle - has 3 sides\n\nCan you find these shapes around you?", 4, 4)
            };

            foreach (var (title, category, content, ageGroup, orderIndex) in lessons)
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Lessons (Title, Category, Content, AgeGroup, OrderIndex)
                    VALUES (@title, @category, @content, @ageGroup, @orderIndex)";
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@category", category);
                command.Parameters.AddWithValue("@content", content);
                command.Parameters.AddWithValue("@ageGroup", ageGroup);
                command.Parameters.AddWithValue("@orderIndex", orderIndex);
                command.ExecuteNonQuery();
            }
        }

        private void SeedVikingQuizzes(SqliteConnection connection)
        {
            // Create Viking Quiz
            using var quizCommand = connection.CreateCommand();
            quizCommand.CommandText = @"
                INSERT INTO Quizzes (Title, Category, AgeGroup)
                VALUES ('Viking Adventure Quiz', 'History', 7)";
            quizCommand.ExecuteNonQuery();
            
            // Get the last inserted row ID
            using var getIdCommand = connection.CreateCommand();
            getIdCommand.CommandText = "SELECT last_insert_rowid()";
            var quizId = Convert.ToInt64(getIdCommand.ExecuteScalar());

            var questions = new[]
            {
                ("Where did the Vikings come from?", new[] { "Norway, Sweden, and Denmark", "France", "Italy", "Spain" }, 0),
                ("What were Viking ships called?", new[] { "Longships", "Speedboats", "Sailboats", "Submarines" }, 0),
                ("What did Vikings use to navigate?", new[] { "The Sun and Stars", "GPS", "Maps", "Compass" }, 0),
                ("What did Vikings call their warriors?", new[] { "Berserkers", "Knights", "Soldiers", "Guards" }, 0),
                ("What did Vikings trade?", new[] { "Furs, honey, and slaves", "Computers", "Cars", "Books" }, 0)
            };

            foreach (var (questionText, options, correctIndex) in questions)
            {
                using var questionCommand = connection.CreateCommand();
                questionCommand.CommandText = @"
                    INSERT INTO Questions (QuizId, QuestionText, Options, CorrectAnswerIndex)
                    VALUES (@quizId, @questionText, @options, @correctIndex)";
                questionCommand.Parameters.AddWithValue("@quizId", quizId);
                questionCommand.Parameters.AddWithValue("@questionText", questionText);
                questionCommand.Parameters.AddWithValue("@options", JsonSerializer.Serialize(options));
                questionCommand.Parameters.AddWithValue("@correctIndex", correctIndex);
                questionCommand.ExecuteNonQuery();
            }
        }

        private void SeedArduinoLessons(SqliteConnection connection)
        {
            var lessons = new[]
            {
                ("What is Arduino?", "Arduino", "Arduino is a small computer board that you can program to make things work!\n\nIt can control lights, motors, sensors, and more. Think of it as a tiny brain for your projects!", 8, 1),
                ("Understanding void setup()", "Arduino", "In Arduino, every program has two main parts:\n\n1. void setup() - This runs ONCE when your Arduino starts\n2. void loop() - This runs OVER AND OVER again\n\nvoid setup() is where you put code that should only happen once, like:\n- Setting up pins\n- Starting communication\n- Initializing variables\n\nExample:\nvoid setup() {\n  pinMode(13, OUTPUT);  // Set pin 13 as output\n  Serial.begin(9600);  // Start communication\n}", 8, 2),
                ("Understanding void loop()", "Arduino", "void loop() is where the magic happens repeatedly!\n\nAfter setup() runs once, loop() runs over and over forever.\n\nExample:\nvoid loop() {\n  digitalWrite(13, HIGH);  // Turn LED on\n  delay(1000);             // Wait 1 second\n  digitalWrite(13, LOW);   // Turn LED off\n  delay(1000);             // Wait 1 second\n}\n\nThis makes an LED blink on and off!", 8, 3),
                ("Your First Arduino Program", "Arduino", "Let's make an LED blink!\n\nConnect an LED to pin 13 (or use the built-in LED).\n\nCode:\nvoid setup() {\n  pinMode(13, OUTPUT);\n}\n\nvoid loop() {\n  digitalWrite(13, HIGH);\n  delay(500);\n  digitalWrite(13, LOW);\n  delay(500);\n}\n\nUpload this code and watch your LED blink!", 8, 4)
            };

            foreach (var (title, category, content, ageGroup, orderIndex) in lessons)
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Lessons (Title, Category, Content, AgeGroup, OrderIndex)
                    VALUES (@title, @category, @content, @ageGroup, @orderIndex)";
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@category", category);
                command.Parameters.AddWithValue("@content", content);
                command.Parameters.AddWithValue("@ageGroup", ageGroup);
                command.Parameters.AddWithValue("@orderIndex", orderIndex);
                command.ExecuteNonQuery();
            }
        }

        private void SeedFusion360Videos(SqliteConnection connection)
        {
            var videos = new[]
            {
                ("Fusion 360 for Beginners - Getting Started", "Fusion360", "d9QalP_W9tY", "Learn the basics of Fusion 360 interface and tools", 10),
                ("Fusion 360 Sketching Tutorial", "Fusion360", "8lON97vDFpw", "Master the sketching tools in Fusion 360", 10),
                ("Fusion 360 3D Modeling Basics", "Fusion360", "vVFYrBClkPc", "Create your first 3D models in Fusion 360", 10),
                ("Fusion 360 Assembly Tutorial", "Fusion360", "xJXlAFEeEww", "Learn how to assemble parts in Fusion 360", 10)
            };

            foreach (var (title, category, videoId, description, ageGroup) in videos)
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO VideoResources (Title, Category, YouTubeVideoId, Description, AgeGroup)
                    VALUES (@title, @category, @videoId, @description, @ageGroup)";
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@category", category);
                command.Parameters.AddWithValue("@videoId", videoId);
                command.Parameters.AddWithValue("@description", description);
                command.Parameters.AddWithValue("@ageGroup", ageGroup);
                command.ExecuteNonQuery();
            }
        }

        // Student methods
        public List<Student> GetStudents()
        {
            var students = new List<Student>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Students ORDER BY Name";
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                students.Add(new Student
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Age = reader.GetInt32(2),
                    CreatedAt = DateTime.Parse(reader.GetString(3))
                });
            }
            
            return students;
        }

        public void AddStudent(Student student)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Students (Name, Age, CreatedAt)
                VALUES (@name, @age, @createdAt)";
            command.Parameters.AddWithValue("@name", student.Name);
            command.Parameters.AddWithValue("@age", student.Age);
            command.Parameters.AddWithValue("@createdAt", student.CreatedAt.ToString("O"));
            command.ExecuteNonQuery();
        }

        // Lesson methods
        public List<Lesson> GetLessons(string? category = null, int? ageGroup = null)
        {
            var lessons = new List<Lesson>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            using var command = connection.CreateCommand();
            var query = "SELECT * FROM Lessons WHERE 1=1";
            
            if (!string.IsNullOrEmpty(category))
            {
                query += " AND Category = @category";
                command.Parameters.AddWithValue("@category", category);
            }
            
            if (ageGroup.HasValue)
            {
                query += " AND AgeGroup = @ageGroup";
                command.Parameters.AddWithValue("@ageGroup", ageGroup.Value);
            }
            
            query += " ORDER BY OrderIndex";
            command.CommandText = query;
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lessons.Add(new Lesson
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Category = reader.GetString(2),
                    Content = reader.GetString(3),
                    AgeGroup = reader.GetInt32(4),
                    OrderIndex = reader.GetInt32(5)
                });
            }
            
            return lessons;
        }

        // Quiz methods
        public List<Quiz> GetQuizzes(string? category = null, int? ageGroup = null)
        {
            var quizzes = new List<Quiz>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            using var command = connection.CreateCommand();
            var query = "SELECT * FROM Quizzes WHERE 1=1";
            
            if (!string.IsNullOrEmpty(category))
            {
                query += " AND Category = @category";
                command.Parameters.AddWithValue("@category", category);
            }
            
            if (ageGroup.HasValue)
            {
                query += " AND AgeGroup = @ageGroup";
                command.Parameters.AddWithValue("@ageGroup", ageGroup.Value);
            }
            
            command.CommandText = query;
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var quiz = new Quiz
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Category = reader.GetString(2),
                    AgeGroup = reader.GetInt32(3)
                };
                
                // Load questions for this quiz
                quiz.Questions = GetQuestions(quiz.Id, connection);
                quizzes.Add(quiz);
            }
            
            return quizzes;
        }

        private List<Question> GetQuestions(int quizId, SqliteConnection connection)
        {
            var questions = new List<Question>();
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Questions WHERE QuizId = @quizId";
            command.Parameters.AddWithValue("@quizId", quizId);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                questions.Add(new Question
                {
                    Id = reader.GetInt32(0),
                    QuizId = reader.GetInt32(1),
                    QuestionText = reader.GetString(2),
                    Options = JsonSerializer.Deserialize<List<string>>(reader.GetString(3)) ?? new(),
                    CorrectAnswerIndex = reader.GetInt32(4)
                });
            }
            
            return questions;
        }

        // Progress methods
        public void SaveProgress(Progress progress)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Progress (StudentId, LessonId, QuizId, Completed, Score, MaxScore, CompletedAt)
                VALUES (@studentId, @lessonId, @quizId, @completed, @score, @maxScore, @completedAt)";
            command.Parameters.AddWithValue("@studentId", progress.StudentId);
            command.Parameters.AddWithValue("@lessonId", progress.LessonId == 0 ? DBNull.Value : progress.LessonId);
            command.Parameters.AddWithValue("@quizId", progress.QuizId.HasValue ? progress.QuizId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@completed", progress.Completed ? 1 : 0);
            command.Parameters.AddWithValue("@score", progress.Score.HasValue ? progress.Score.Value : DBNull.Value);
            command.Parameters.AddWithValue("@maxScore", progress.MaxScore.HasValue ? progress.MaxScore.Value : DBNull.Value);
            command.Parameters.AddWithValue("@completedAt", progress.CompletedAt.ToString("O"));
            command.ExecuteNonQuery();
        }

        public List<Progress> GetStudentProgress(int studentId)
        {
            var progressList = new List<Progress>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Progress WHERE StudentId = @studentId ORDER BY CompletedAt DESC";
            command.Parameters.AddWithValue("@studentId", studentId);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                progressList.Add(new Progress
                {
                    Id = reader.GetInt32(0),
                    StudentId = reader.GetInt32(1),
                    LessonId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                    QuizId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Completed = reader.GetInt32(4) == 1,
                    Score = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    MaxScore = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    CompletedAt = reader.IsDBNull(7) ? DateTime.MinValue : DateTime.Parse(reader.GetString(7))
                });
            }
            
            return progressList;
        }

        // Video methods
        public List<VideoResource> GetVideoResources(string? category = null, int? ageGroup = null)
        {
            var videos = new List<VideoResource>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            using var command = connection.CreateCommand();
            var query = "SELECT * FROM VideoResources WHERE 1=1";
            
            if (!string.IsNullOrEmpty(category))
            {
                query += " AND Category = @category";
                command.Parameters.AddWithValue("@category", category);
            }
            
            if (ageGroup.HasValue)
            {
                query += " AND AgeGroup = @ageGroup";
                command.Parameters.AddWithValue("@ageGroup", ageGroup.Value);
            }
            
            command.CommandText = query;
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                videos.Add(new VideoResource
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Category = reader.GetString(2),
                    YouTubeVideoId = reader.GetString(3),
                    Description = reader.GetString(4),
                    AgeGroup = reader.GetInt32(5)
                });
            }
            
            return videos;
        }
    }
}

