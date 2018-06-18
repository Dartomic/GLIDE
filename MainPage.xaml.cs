using ObservableImageTest.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SQLite.Net;
using SQLite.Net.Attributes;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;

namespace ObservableImageTest
{
    public sealed partial class MainPage : Page
    {
        // Topic Stuff
        private List<TopicModel> TopicsList;
        private List<int> ToStudy = new List<int>(); // Contains ID's for the current study session.

        // Lesson Stuff
        private List<LessonModel> LessonsList;
        private ObservableCollection<LessonModel> LessonContent;

        // Answers Stuff
        private List<AnswerModel> AnswersList;
        private ObservableCollection<AnswerModel> AnswersContent;

        // Problems Stuff
        private List<ProblemModel> ProblemList;
        private ObservableCollection<ProblemModel> ProblemContent;

        // For communication between methods
        GlobalVariables globals = new GlobalVariables();

        public MainPage()
        {
            this.InitializeComponent();

            // Topic
            TopicsList = new List<TopicModel>();

            // Lesson
            LessonContent = new ObservableCollection<LessonModel>();
            LessonsList = new List<LessonModel>();

            // Problem
            ProblemContent = new ObservableCollection<ProblemModel>();
            ProblemList = new List<ProblemModel>();

            //Answer
            AnswersContent = new ObservableCollection<AnswerModel>();
            AnswersList = new List<AnswerModel>();

            SetDisplay();
        }

        // For the custom window title bar.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Over ride the title bar default colors
            ExtendAcrylicIntoTitleBar();
        }
        private void ExtendAcrylicIntoTitleBar()
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveForegroundColor = Colors.White;
        }

        private void SetDisplay()
        {
            const int ZERO = 0;

            // Topics
            TopicsList = TopicManager.GetTopics(); // Allows default values to be used, or replaced with saved values.
            LoadTopicIDs();

            // DataBase
            CheckForFile();
            if (globals.FileExists == false)
            {
                CreateDB();
            }
            else
            {
                DbToList();
            }

            // Problem
            ProblemList = ProblemManager.GetProblems();

            // Answer
            AnswersList = AnswerManager.GetAll();

            // Lesson
            LessonsList = LessonManager.GetLessons();

            // For decisions
            globals.ProblemInitializer = ZERO;
            globals.AnswerInitializer = ZERO;
            globals.LessonInitializer = ZERO;
            globals.Wait = ZERO;
            globals.WasAnswered = false;



            /* Learning Start */
            // LoadTopicIDs();

            // Lesson Section

            // Problem Section
            FirstProblem();
            LoadProblem();

            // Answer Section
            FirstAnswers();
            AnswerProblemCompare();
            LoadAnswers();

            // Lesson Section

        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var answer = (AnswerModel)e.ClickedItem;
            globals.CheckCorrect = answer.AnswerCorrect;
            globals.WasAnswered = true;


        }

        private void NExt_Click(object sender, RoutedEventArgs e)
        {
            const int ZERO = 0;
            const int ONE = 1;
            string reveal = globals.RevealAnswer;
            double correctProblems;

            // Gives feedback after answer, or goes to next problem after feedback.
            if (globals.Wait == ZERO)
            {
                globals.Wait = globals.Wait + ONE;

                if (globals.WasAnswered == true)
                {
                    if (globals.CheckCorrect == true)
                    {
                        // I need to increment a counter of correct answers here for the topic,
                        // and zero the variable somewhere else after the topic is calculated.

                        correctProblems = TopicsList.ElementAt(globals.TopicIndex).Num_Correct;
                        correctProblems = correctProblems + ONE;
                        TopicsList.ElementAt(globals.TopicIndex).Num_Correct = correctProblems;

                        Result.Text = "Good Job!";
                    }
                    else
                    {
                        Result.Text = ($"Sorry, the correct answer was {reveal}");
                    }
                }
                else
                {
                    Result.Text = ($"Sorry, the correct answer was {reveal}");
                }

            }
            else
            {
                if (globals.ProblemsDone != true)
                {
                    globals.WasAnswered = false;
                    Result.Text = ($" ");
                    NextProblem();
                    if (globals.ProblemsDone != true)
                    {
                        LoadProblem();
                        NextAnswers();
                        AnswerProblemCompare();
                        LoadAnswers();
                        globals.Wait = ZERO;
                    }

                }
                else
                {
                    Result.Text = ($"Nothing left to study today. Check back tomorrow!");
                }

            }

        }

        // Check for DB

        // Create DB from default values

        // Load stored values to list from DB

        // ToStudy ID's Section
        private void LoadTopicIDs()
        {
            const int ZERO = 0;
            const int ONE = 1;
            int index;

            // Date section
            DateTime today = DateTime.Now;
            DateTime topicDate;
            int dateCompare;
            string dateAsString;

            // Process the list of AllTopics into:

            // Retry Studied TopicID's section
            // I plan on changing this so that it resets the values of the topic, back to how they were as new values,
            // IF the retrieval calculation is equal to, or less than, 90%. I chose that, because a grade of 'A' is what the users want to get.
            // Right now I just have it here so it hopefully gets studied at the Primacy end of the Serial-Position Effect. Unless I forget, 
            // then it shouldn't even be like this long enough to be used.
            index = ZERO;
            while (index < TopicsList.Count)
            {
                if (TopicsList.ElementAt(index).Top_Studied == true)
                {
                    dateAsString = TopicsList.ElementAt(index).Next_Date;
                    topicDate = DateTime.Parse(dateAsString);
                    dateCompare = DateTime.Compare(topicDate, today);

                    if (dateCompare < ZERO)
                    {
                        // Lessons and problems both depend on the ID of the topic, but answers depend on the problem ID.
                        ToStudy.Add(index);
                    }
                }

                index = index + ONE;
            }


            // Studied TopicID's scheduled for today section
            index = ZERO;
            while (index < TopicsList.Count)
            {
                if (TopicsList.ElementAt(index).Top_Studied == true)
                {
                    dateAsString = TopicsList.ElementAt(index).Next_Date;
                    topicDate = DateTime.Parse(dateAsString);
                    dateCompare = DateTime.Compare(topicDate, today);

                    if (dateCompare == ZERO)
                    {
                        // Lessons and problems both depend on the ID of the topic, but answers depend on the problem ID.
                        ToStudy.Add(index);
                    }
                }

                index = index + ONE;
            }


            // New Topic ID's section
            index = ZERO;
            while (index < TopicsList.Count)
            {
                if (TopicsList.ElementAt(index).Top_Studied == false)
                {
                    // Lessons and problems both depend on the ID of the topic, but answers depend on the problem ID.
                    ToStudy.Add(index);
                }

                index = index + ONE;
            }

            globals.TopicID = ToStudy.ElementAt(ZERO);
            globals.TopicIndex = ZERO;
        }

        // Problems Section
        private void FirstProblem()
        {
            /* Original code */
            const int ZERO = 0;
            const int ONE = 1;

            /* Original code */
            int topicID = ToStudy.ElementAt(globals.TopicIndex); // A single element holding a value of two for this test.            
            globals.ProblemIndex = ZERO;
            int problemTopic = ProblemList.ElementAt(globals.ProblemIndex).TopicID;

            while (problemTopic != topicID)
            {
                if (globals.ProblemIndex < ProblemList.Count)
                {
                    globals.ProblemIndex = globals.ProblemIndex + ONE;
                }
                if (globals.ProblemIndex >= ProblemList.Count)
                {
                    globals.ProblemIndex = ZERO; // Only for testing. Needs to set a check variable for program to show "Nothing to study."
                }
                problemTopic = ProblemList.ElementAt(globals.ProblemIndex).TopicID;
            }
        }
        private void NextProblem()
        {
            const double ZERO_DOUBLE = 0;
            const int ZERO_INT = 0;
            const int ONE = 1;

            globals.ProblemsDone = false; // Set this value in a global variable so that button itself will stop executing code. Set it to true anytime an index for a list is !< the list.

            int topicID;
            int toStudyCount;
            int topicIndex;

            int numProblems;
            int problemTopic;
            int problemIndex;

            /* Initialization */
            // Size of lists
            toStudyCount = ToStudy.Count;
            numProblems = ProblemList.Count;

            // Topic section
            topicIndex = globals.TopicIndex; // Make sure this is used before this method too.
            topicID = ToStudy.ElementAt(topicIndex);

            // Problem Section           
            problemIndex = globals.ProblemIndex; // Make sure to set this, probably in FirstProblem.
            problemTopic = ProblemList.ElementAt(problemIndex).TopicID;

            if (problemIndex < numProblems)
            {
                globals.ProblemIndex = globals.ProblemIndex + ONE;
                problemIndex = globals.ProblemIndex;
            }
            else
            {
                globals.ProblemIndex = ZERO_INT;
                problemIndex = globals.ProblemIndex;
            }

            if (problemIndex >= numProblems)
            {
                globals.ProblemIndex = ZERO_INT;
                problemIndex = globals.ProblemIndex;
            }

            problemTopic = ProblemList.ElementAt(problemIndex).TopicID;

            // The new problem may exist for a different topic. This means the topic's problems have all ran. 
            // If so, the Calculate methods must be called. 
            // Also, the topicID must increment. 
            // If the topicID changes to the next one up, then the Problem Index must be set to ZERO, and a loop needs to run.

            // The loop must increment the ProblemIndex.
            // The loop must check if the TopicID of a problem, matches the current topic ID value in ToStudy.
            // The loop must exit, once these two ID's match.

            if (problemTopic != topicID)
            {
                CalculateLearning();
                SaveProgress();
                TopicsList.ElementAt(globals.TopicIndex).Num_Correct = ZERO_DOUBLE;

                if (topicIndex < toStudyCount)
                {
                    // Topics can not increment in a loop, because no single topic in ToStudy should be skipped.
                    globals.TopicIndex = globals.TopicIndex + ONE;
                    topicIndex = globals.TopicIndex;

                    if (topicIndex < toStudyCount)
                    {
                        topicID = ToStudy.ElementAt(topicIndex);
                    }

                }

                if (topicIndex >= toStudyCount)
                {
                    globals.ProblemsDone = true; // to disable buttons.
                    //   Display "No more material for todays study session."
                }

                // If the topic incremented, then the first problem with the matching topic ID needs to be found.

                if (globals.ProblemsDone == false)
                {
                    if (problemTopic != topicID)
                    {
                        globals.ProblemIndex = ZERO_INT;
                        problemIndex = globals.ProblemIndex;
                        problemTopic = ProblemList.ElementAt(problemIndex).TopicID;

                        while (problemTopic != topicID)
                        {

                            globals.ProblemIndex = globals.ProblemIndex + ONE;
                            problemIndex = globals.ProblemIndex;
                            problemTopic = ProblemList.ElementAt(problemIndex).TopicID;
                        }
                    }
                }
            }

            globals.TopicID = problemTopic;
            globals.ProblemID = ProblemList.ElementAt(problemIndex).ProblemID;
        }
        private void LoadProblem()
        {
            const int ZERO = 0;
            const int ONE = 1;
            int problemIndex = globals.ProblemIndex;

            // Get the problem ID into the global variable for the check when loading the answers later.
            //globals.ProblemID 

            // Copy the problem values so I can use them
            string problemPath = ProblemList.ElementAt(globals.ProblemIndex).ProblemPath;
            int problemID = ProblemList.ElementAt(globals.ProblemIndex).ProblemID;
            globals.ProblemID = problemID;


            if (globals.ProblemInitializer == ZERO)
            {
                ProblemContent.Add(new ProblemModel { ProblemPath = problemPath });

                globals.ProblemInitializer = ONE;
            }
            else
            {
                ProblemContent.RemoveAt(0);
                ProblemContent.Add(new ProblemModel { ProblemPath = problemPath });
            }
        }

        // Answers Section
        private void FirstAnswers()
        {
            const int ZERO = 0;
            const int ONE = 1;
            const int TWO = 2;

            globals.AnswerIndexOne = ZERO;
            globals.AnswerIndexTwo = ONE;
            globals.AnswerIndexThree = TWO;

        }
        private void NextAnswers()
        {
            const int ONE = 1;

            // Every problem has three answer choices. All answer choices are in a single list.
            // When the problem is incremented to the next problem, then the current three answer choices need to
            // increment to the next three answer choices. Therefore; the value of the index for the first answer choice, 
            // of the next three problems, is one greater than the value of the index for the previous THIRD answer choice,
            // before that third answer choice was randomly shuffled into the observable collection.
            globals.AnswerIndexOne = globals.AnswerIndexThree + ONE;

            // Now that "AnswerIndexOne" is greater by a value of 1 than what "AnswerIndexThree" was for
            // the previous problem, AnswerIndexTwo needs to be greater by a value of 1, than the new value
            // of "AnswerIndexOne."
            globals.AnswerIndexTwo = globals.AnswerIndexOne + ONE;

            // Just like "AnswerIndexTwo" must be greater than "AnswerIndexOne," by a value of 1; "AnswerIndexThree" should
            // be greater than "AnswerIndexTwo," by a value of 1. 
            globals.AnswerIndexThree = globals.AnswerIndexTwo + ONE;





            // Move these to be called from the button instead.

        }
        private void AnswerProblemCompare()
        {
            const int ZERO = 0;
            const int ONE = 1;
            const int TWO = 2;

            globals.AnswerIndexOne = ZERO;
            globals.AnswerIndexTwo = globals.AnswerIndexOne + ONE;
            globals.AnswerIndexThree = globals.AnswerIndexTwo + ONE;

            // Index
            int indexOne = globals.AnswerIndexOne;
            int indexTwo = globals.AnswerIndexTwo;
            int indexThree = globals.AnswerIndexThree;

            // Problem
            int problemID = globals.ProblemID;

            // Answer
            int answerOneID = AnswersList.ElementAt(indexOne).ProblemID;

            while (answerOneID != problemID)
            {
                // Increment answer index
                globals.AnswerIndexOne = globals.AnswerIndexThree + ONE;
                globals.AnswerIndexTwo = globals.AnswerIndexOne + ONE;
                globals.AnswerIndexThree = globals.AnswerIndexTwo + ONE;

                indexOne = globals.AnswerIndexOne;
                indexThree = globals.AnswerIndexThree;

                // If indexThree greater than last index value of the answer list, reset answer index to zero. 
                indexThree = globals.AnswerIndexThree;
                if (indexThree >= AnswersList.Count)
                {
                    globals.AnswerIndexOne = ZERO;
                    globals.AnswerIndexTwo = ONE;
                    globals.AnswerIndexThree = TWO;
                }

                // Get the new answer ID to check
                indexOne = globals.AnswerIndexOne;
                answerOneID = AnswersList.ElementAt(indexOne).ProblemID;

            }
        }
        private void LoadAnswers()
        {
            const int ZERO = 0;
            const int ONE = 1;
            const int TWO = 2;


            int indexOne = globals.AnswerIndexOne;
            int indexTwo = globals.AnswerIndexTwo;
            int indexThree = globals.AnswerIndexThree;

            // Shuffle the copied index values of the answers to use.
            var indexArray = new int[] { indexOne, indexTwo, indexThree };
            new Random().Shuffle(indexArray);

            int valueOne = indexArray[ZERO];
            int valueTwo = indexArray[ONE];
            int valueThree = indexArray[TWO];

            /* Assigning letters to the randomized answers that display. "a)", "b)", and "c)", 
             * need to display in alphabetic order 
             * The number of the labels for "valueNumber" and "letterNumber" should match, because they are used for the same answer for each of the labels.
             */
            string letterOne = "a) ";
            string letterTwo = "b) ";
            string letterThree = "c) ";

            // This is used to display which answer choice is correct, if the user selects the incorrect answer.
            if (indexOne == valueOne)
            {
                globals.RevealAnswer = letterOne;
            }
            if (indexOne == valueTwo)
            {
                globals.RevealAnswer = letterTwo;
            }
            if (indexOne == valueThree)
            {
                globals.RevealAnswer = letterThree;
            }

            // Copy the answer values so I can use them
            string answerImageOne = AnswersList.ElementAt(valueOne).AnswerPath;
            bool correctIncorrectOne = AnswersList.ElementAt(valueOne).AnswerCorrect;

            string answerImageTwo = AnswersList.ElementAt(valueTwo).AnswerPath;
            bool correctIncorrectTwo = AnswersList.ElementAt(valueTwo).AnswerCorrect;

            string answerImageThree = AnswersList.ElementAt(valueThree).AnswerPath;
            bool correctIncorrectThree = AnswersList.ElementAt(valueThree).AnswerCorrect;


            if (globals.AnswerInitializer == ZERO)
            {
                AnswersContent.Add(new AnswerModel { AnswerPath = answerImageOne, DisplayLetter = letterOne, AnswerCorrect = correctIncorrectOne });
                AnswersContent.Add(new AnswerModel { AnswerPath = answerImageTwo, DisplayLetter = letterTwo, AnswerCorrect = correctIncorrectTwo });
                AnswersContent.Add(new AnswerModel { AnswerPath = answerImageThree, DisplayLetter = letterThree, AnswerCorrect = correctIncorrectThree });

                globals.AnswerInitializer = ONE;
            }
            else
            {

                AnswersContent.RemoveAt(0);
                AnswersContent.Add(new AnswerModel { AnswerPath = answerImageOne, DisplayLetter = letterOne, AnswerCorrect = correctIncorrectOne });

                AnswersContent.RemoveAt(0);
                AnswersContent.Add(new AnswerModel { AnswerPath = answerImageTwo, DisplayLetter = letterTwo, AnswerCorrect = correctIncorrectTwo });

                AnswersContent.RemoveAt(0);
                AnswersContent.Add(new AnswerModel { AnswerPath = answerImageThree, DisplayLetter = letterThree, AnswerCorrect = correctIncorrectThree });
            }
        }

        // Lesson Section
        private void LessonProblemCompare()
        {
            const int ZERO = 0;
            const int ONE = 1;
            globals.LessonIndex = ZERO;
            int index = globals.LessonIndex;
            int problemTopicID = globals.TopicID;
            int LessonID = LessonsList.ElementAt(index).LessonID;

            // Will cause infinite loop if topic ID for a problem does not match any lesson ID.
            while (LessonID != problemTopicID)
            {
                // Increment answer index
                globals.LessonIndex = globals.LessonIndex + ONE;

                index = globals.LessonIndex;

                // If index greater than or equal to last index value of the lesson list, reset answer index to zero.
                if (index >= LessonsList.Count)
                {
                    globals.LessonIndex = ZERO;
                }

                // Get the new lesson ID to check
                index = globals.LessonIndex;
                LessonID = LessonsList.ElementAt(index).LessonID;
            }
        }
        private void LoadLesson()
        {
            const int ZERO = 0;
            const int ONE = 1;
            int index = globals.LessonIndex;
            string lessonImage = LessonsList.ElementAt(index).LessonPath;


            if (globals.LessonInitializer == ZERO)
            {
                LessonContent.Add(new LessonModel { LessonPath = lessonImage });

                globals.LessonInitializer = ONE;
            }
            else
            {

                AnswersContent.RemoveAt(0);
                LessonContent.Add(new LessonModel { LessonPath = lessonImage });
            }

        }

        // Calculate Section
        private void CalculateLearning()
        {
            const double ONE = 1;
            AddRepetition();

            double ithRepetition = TopicsList.ElementAt(globals.TopicIndex).Top_Repetition;
            if (ithRepetition == ONE)
            {
                TopicDifficulty();
            }

            IntervalTime();
            EngramStability();
            EngramRetrievability();
            ProcessDate();
        }
        private void AddRepetition()
        {
            const double ONE = 1;
            double ithRepetition = TopicsList.ElementAt(globals.TopicIndex).Top_Repetition;

            ithRepetition = ithRepetition + ONE;
            TopicsList.ElementAt(globals.TopicIndex).Top_Repetition = ithRepetition;
        }
        private void TopicDifficulty()
        {
            // Since intervalTime multiplies against difficulty, and difficulty is set only once
            // then a topic could be scheduled every day for a long time if too close to 1.0, and too 
            // far apart if above 2.5

            const double LOW_DIFFICULTY = 2.5;
            const double HIGH_DIFFICULTY = 1.3;
            double rise = LOW_DIFFICULTY - HIGH_DIFFICULTY;
            double totalProblems = TopicsList.ElementAt(globals.TopicIndex).Num_Problems;
            double correctProblems = TopicsList.ElementAt(globals.TopicIndex).Num_Correct;
            double run = totalProblems;
            double slope = rise / run;
            double difficulty = (slope * correctProblems) + HIGH_DIFFICULTY; // Slope-Intercept formula y = mx + b

            TopicsList.ElementAt(globals.TopicIndex).Top_Difficulty = difficulty; // Write difficulty to student record file Difficulty column
        }
        private void IntervalTime()
        {
            const double ONE = 1;
            const double SINGLE_DAY = 1440; // 1440 is the quatity in minutes of a day. I'm using minutes, instead of whole days, to be more precise.
            double difficulty = TopicsList.ElementAt(globals.TopicIndex).Top_Difficulty;
            double ithRepetition = TopicsList.ElementAt(globals.TopicIndex).Top_Repetition;
            double intervalRemaining = TopicsList.ElementAt(globals.TopicIndex).Interval_Remaining;
            double intervalLength = TopicsList.ElementAt(globals.TopicIndex).Interval_Length;

            //     Second repetition will occur the next day. 
            //	   Although, the research document does not precisely
            //	   state a time frame until the second repetition. The 
            //	   values of the two variables may need to be changed, 
            //	   if the spacing is too far apart.

            if (ithRepetition == ONE)
            {
                // The researech document says that s == r @ 1st repetition
                intervalRemaining = SINGLE_DAY;
                intervalLength = SINGLE_DAY;
            }
            else
            {
                intervalLength = intervalLength * difficulty;
            }

            intervalRemaining = intervalLength;
            TopicsList.ElementAt(globals.TopicIndex).Interval_Length = intervalLength; // Write intervalLength to student record Interval.
            TopicsList.ElementAt(globals.TopicIndex).Interval_Remaining = intervalRemaining; // Write remainingTime to student record file RTime column
        }
        private void EngramStability()
        {
            const double KNOWLEDGE_LINK = -0.0512932943875506;
            const double NEGATIVE_ONE = -1;

            // remainingTime and intervalLength represent the variables r and s, respectively, from the research document.
            double intervalRemaining = TopicsList.ElementAt(globals.TopicIndex).Interval_Remaining;
            double intervalLength = TopicsList.ElementAt(globals.TopicIndex).Interval_Length;
            double stabilityOfEngram;

            stabilityOfEngram = (NEGATIVE_ONE * intervalLength) / KNOWLEDGE_LINK; // S = -s/ln(K), where K = 0.95, and the natural logarithm of K equals KNOWLEDGE_LINK.            
            TopicsList.ElementAt(globals.TopicIndex).Engram_Stability = stabilityOfEngram; // Write Stability to student record file Stability column
        }
        private void EngramRetrievability()
        {
            const double NEGATIVE_ONE = -1;
            double intervalLength = TopicsList.ElementAt(globals.TopicIndex).Interval_Length;
            double intervalRemaining = TopicsList.ElementAt(globals.TopicIndex).Interval_Remaining;
            double stabilityOfEngram = TopicsList.ElementAt(globals.TopicIndex).Engram_Stability;
            double power = NEGATIVE_ONE * (intervalLength - intervalRemaining) / stabilityOfEngram;
            double retrievability = Math.Exp(power);

            TopicsList.ElementAt(globals.TopicIndex).Engram_Retrievability = retrievability;
        }
        private void ProcessDate()
        {
            int TopicIndex = globals.TopicIndex;
            double intervalLength = TopicsList.ElementAt(globals.TopicIndex).Interval_Length;
            double intervalRemaining = TopicsList.ElementAt(globals.TopicIndex).Interval_Remaining;
            double days = Convert.ToInt32(intervalLength / intervalRemaining);
            DateTime today = DateTime.Now;
            DateTime nextDate = today.AddDays(days);
            string nextDateString = nextDate.ToString("d");

            TopicsList.ElementAt(globals.TopicIndex).Next_Date = nextDateString;
            if (TopicsList.ElementAt(globals.TopicIndex).Top_Studied == false)
            {
                TopicsList.ElementAt(globals.TopicIndex).Top_Studied = true;
            }
        }


        /* DataBase Section */

        // Save
        private void SaveProgress()
        {

        }

        // Check for file.
        private async void CheckForFile()
        {
            var filename = "Topics.db";
            var folder = ApplicationData.Current.LocalFolder;

            // This returns null if it doesn't exist
            var file = await folder.TryGetItemAsync(filename);

            if (file != null)
            {
                // File is deleted if it does exist
                globals.FileExists = true;
            }
            else
            {
                globals.FileExists = false;
            }
        }

        // If file does NOT exist.
        public async void CreateDB()
        {
            using (SQLiteConnection conn = await OpenRecreateConnection())
            {
                conn.CreateTable<TopicModel>();
                foreach (var info in TopicsList)
                {
                    conn.InsertOrReplace(info);
                }
            }
        }

        // If file DOES exist
        // THEN Get every value, for each element, into the list, and replace the initial values.
        public async void DbToList()
        {
            using (SQLiteConnection conn = await OpenRecreateConnection())
            {
                var infos = from p in conn.Table<TopicModel>() select p;

                /* Use one of these for every value for each element of the topic list. */
                var topIDs = string.Join(",", infos.Select(t => $"{t.Top_ID.ToString()}"));
                var courseIDs = string.Join(",", infos.Select(t => $"{t.Course_ID.ToString()}"));
                var topsStudied = string.Join(",", infos.Select(t => $"{t.Top_Studied.ToString()}"));

                var nextDates = string.Join(",", infos.Select(t => $"{t.Next_Date}"));
                var firstDates = string.Join(",", infos.Select(t => $"{t.First_Date}"));

                var numProblems = string.Join(",", infos.Select(t => $"{t.Num_Problems.ToString()}"));
                var numCorrect = string.Join(",", infos.Select(t => $"{t.Num_Correct.ToString()}"));

                var topDifficulties = string.Join(",", infos.Select(t => $"{t.Top_Difficulty.ToString()}"));
                var topReps = string.Join(",", infos.Select(t => $"{t.Top_Repetition.ToString()}"));
                var intervalRemains = string.Join(",", infos.Select(t => $"{t.Interval_Remaining.ToString()}"));

                var intervalLengths = string.Join(",", infos.Select(t => $"{t.Interval_Length.ToString()}"));
                var engramStabilities = string.Join(",", infos.Select(t => $"{t.Engram_Stability.ToString()}"));
                var engramRetrievabilities = string.Join(",", infos.Select(t => $"{t.Engram_Retrievability.ToString()}"));


                /* Splitting */
                string[] topIdEntries = topIDs.Split(',');
                string[] courseIDEntries = courseIDs.Split(',');
                string[] topStudiedEntries = topsStudied.Split(',');

                string[] nextDateEntries = nextDates.Split(',');
                string[] firstDateEntries = firstDates.Split(',');

                string[] numProblemEntries = numProblems.Split(',');
                string[] numCorrectEntries = numCorrect.Split(',');

                string[] topDifficultEntries = topDifficulties.Split(',');
                string[] topRepEntries = topReps.Split(',');
                string[] intervalRemainingEntries = intervalRemains.Split(',');

                string[] intervalLengthEntries = intervalLengths.Split(',');
                string[] engramStabilityEntries = engramStabilities.Split(',');
                string[] engramRetrievabilityEntries = engramRetrievabilities.Split(',');


                /* 
                Naming the variables here, so that program does not loop the naming of new memory units. 
                Not sure if it would happen that way, but just in case. 
                */
                int topID;
                int courseID;
                bool topStudied;

                string nextDate;
                string firstDate;

                double numProblem;
                double correctNum;

                double topDifficulty;
                double topRep;
                double intervalRemaining;

                double intervalLength;
                double engramStability;
                double engramRetrievability;
                

                // Using a single loop, so that this stage may run quicker, than if a loop occured for each variable of each element.
                int index = 0;
                string[] Entries = topIDs.Split(','); // This one is just for the size of the list.
                int lengthEntries;
                foreach (var attribute in topIDs)
                {
                    lengthEntries = Entries.Length;
                    if (index < lengthEntries)
                    {
                        /* Preparing for list. */
                        topID = Convert.ToInt32(topIdEntries[index]);
                        courseID = Convert.ToInt32(courseIDEntries[index]);
                        topStudied = Convert.ToBoolean(topStudiedEntries[index]);

                        nextDate = nextDateEntries[index];
                        firstDate = firstDateEntries[index];

                        numProblem = Convert.ToDouble(numProblemEntries[index]);
                        correctNum = Convert.ToDouble(numCorrectEntries[index]);

                        topDifficulty = Convert.ToDouble(topDifficultEntries[index]);
                        topRep = Convert.ToDouble(topRepEntries[index]);
                        intervalRemaining = Convert.ToDouble(intervalRemainingEntries[index]);

                        intervalLength = Convert.ToDouble(intervalLengthEntries[index]);
                        engramStability = Convert.ToDouble(engramStabilityEntries[index]);
                        engramRetrievability = Convert.ToDouble(engramRetrievabilityEntries[index]);

                        /* Loading to list */
                        TopicsList.ElementAt(index).Top_ID = topID;
                        TopicsList.ElementAt(index).Course_ID = courseID;
                        TopicsList.ElementAt(index).Top_Studied = topStudied;

                        TopicsList.ElementAt(index).Next_Date = nextDate;
                        TopicsList.ElementAt(index).First_Date = firstDate;

                        TopicsList.ElementAt(index).Num_Problems = numProblem;
                        TopicsList.ElementAt(index).Num_Correct = numCorrect;

                        TopicsList.ElementAt(index).Top_Difficulty = topDifficult;
                        TopicsList.ElementAt(index).Top_Repetition = topRep;
                        TopicsList.ElementAt(index).Interval_Remaining = intervalRemaining;

                        TopicsList.ElementAt(index).Interval_Length = intervalLength;
                        TopicsList.ElementAt(index).Engram_Stability = engramStability;
                        TopicsList.ElementAt(index).Engram_Retrievability = engramRetrievability;
                    }

                    index++;
                }
            }

        }

        /* Open or recreate the database */
        private async Task<SQLiteConnection> OpenRecreateConnection(bool ReCreate = false)
        {
            var filename = "Topics.db";
            var folder = ApplicationData.Current.LocalFolder;

            // Delete current database if it is to be recreated
            if (ReCreate)
            {
                // This returns null if it doesn't exist
                var file = await folder.TryGetItemAsync(filename);

                if (file != null)
                {
                    // File is deleted if it does exist
                    await file.DeleteAsync();
                }
            }
            // SQLite needs a path to connect with the database.
            var sqlpath = Path.Combine(folder.Path, filename);

            // Now the database is created, by creating the connection. SQLitePlatformWinRT is needed here, so 
            // that SQLite knows what platform the database is being managed within.
            return new SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), sqlpath);

        }
    }
}
