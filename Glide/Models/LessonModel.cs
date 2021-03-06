﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Program Title: Glide
// Class File: LessonModel.cs
// Author: Herbert Smith


/* If a Topic ID in the TopicModel class file exists, then the lesson for that Topic ID needs to exist here, or the program will crash when it reaches that topic. */

namespace Glide.Models
{
    public class LessonModel
    {
        public int CourseID { get; set; }
        // The lesson ID is the same as the topic ID, since there is one lesson per-topic.
        public int LessonID { get; set; }
        public string LessonPath { get; set; }
    }

    public class LessonManager
    {

        public static List<LessonModel> GetLessons()
        {
            var Lessons = new List<LessonModel>();

            Lessons.Add(new LessonModel { CourseID = 0, LessonID = 0, LessonPath = "Assets/LessonImages/0.png" });
            Lessons.Add(new LessonModel { CourseID = 0, LessonID = 1, LessonPath = "Assets/LessonImages/1.png" });
            Lessons.Add(new LessonModel { CourseID = 0, LessonID = 2, LessonPath = "Assets/LessonImages/2.png" });

            return Lessons;
        }
    }
}
