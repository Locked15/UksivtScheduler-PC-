﻿using System;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using UksivtScheduler_PC.Controls;
using UksivtScheduler_PC.Classes.General;
using UksivtScheduler_PC.Classes.SiteParser;
using UksivtScheduler_PC.Classes.DocumentParser;
using UksivtScheduler_PC.Classes.ScheduleElements;

/// <summary>
/// Область кода с окном вывода расписания.
/// </summary>
namespace UksivtScheduler_PC.Windows
{
    /// <summary>
    /// Логика взаимодействия для FinalSchedule.xaml.
    /// </summary>
    public partial class FinalSchedule : Window
    {
        #region Область: Поля.
        /// <summary>
        /// Поле, содержащее оригинальное расписание.
        /// </summary>
        private DaySchedule originalSchedule;

        /// <summary>
        /// Поле, содержащее расписание с заменами.
        /// </summary>
        private DaySchedule scheduleWithChanges;

        /// <summary>
        /// Поле, содержащее родительское окно для данного окна.
        /// </summary>
        private DaySelector parent;
        #endregion

        #region Область: Конструктор.
        /// <summary>
        /// Конструктор класса.
        /// </summary>
        /// <param name="prefix">Префикс группы.</param>
        /// <param name="group">Название группы.</param>
        /// <param name="day">День для получения расписания.</param>
        /// <param name="parent">Родительское окно.</param>
        public FinalSchedule(String prefix, String group, String day, DaySelector parent)
        {
            this.parent = parent;

            InitializeComponent();
            InitizlizeFields(prefix, group, day);
        }
        #endregion

        #region Область: События.
        /// <summary>
        /// Событие, происходящее при нажатии на "Schedule_GoBack'.
        /// <br/>
        /// Возвращает пользователя на предыдущее окно.
        /// </summary>
        /// <param name="sender">Элемент, вызвавший событие.</param>
        /// <param name="e">Аргументы события.</param>
        public void GoBackClick(Object sender, EventArgs e)
        {
            parent.Show();
            Close();
        }

        /// <summary>
        /// Событие, происходящее при закрытии окна.
        /// <br/>
        /// Нужно для освобождения памяти.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие.</param>
        /// <param name="e">Аргументы события.</param>
        private void Window_Closed(Object sender, EventArgs e)
        {
            parent.Parent.Parent.Close();
            parent.Parent.Close();
            parent.Close();
        }
        #endregion

        #region Область: Методы.
        /// <summary>
        /// Метод для инициализации полей.
        /// </summary>
        /// <param name="prefix">Префикс группы.</param>
        /// <param name="group">Название группы.</param>
        /// <param name="day">День для получения расписания.</param>
        private async void InitizlizeFields(String prefix, String group, String day)
        {
            //Получаем оригинальное расписание:
            originalSchedule = Helper.GetWeekSchedule(prefix, group).Days[day.GetIndexByDay()];

            //Операции занимают много времени, выносим в отдельный поток:
            await Task.Run(() =>
            {
                //Парсим сайт и получаем нужный элемент:
                Parser parse = new Parser();
                ChangeElement change = parse.ParseAvailableNodes().TryToFindElementByNameOfDayWithoutPreviousWeeks(day);

                if (change.CheckHavingChanges())
                {
                    //Парсим файл с заменами и получаем измененное расписание:
                    String url = Helper.GetDownloadableFileLink(change.LinkToDocument);
                    ChangesReader reader = new ChangesReader(Helper.DownloadFileFromURL(url));
                    scheduleWithChanges = reader.GetDayScheduleWithChanges(day, group, originalSchedule);

                    scheduleWithChanges.Lessons.RemoveAll(lesson => !lesson.CheckHaveValue());

                    InsertData(scheduleWithChanges);
                }

                else
                {
                    originalSchedule.Lessons.RemoveAll(lesson => !lesson.CheckHaveValue());

                    InsertData(originalSchedule);

                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Замены для текущего дня не обнаружены.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            });
        }

        /// <summary>
        /// Метод для вставки данных о расписании в элементы окна.
        /// </summary>
        /// <param name="schedule">Расписание для вставки в таблицу.</param>
        private void InsertData(DaySchedule schedule)
        {
            /* Операция выполняется в отдельном потоке, ...
               ... поэтому операции с UI проводятся через "Dispatcher". */
            foreach (Lesson lesson in schedule.Lessons)
            {
                Int32 number = lesson.Number;
                String name = lesson.Name;
                String teacher = lesson.Teacher;
                String place = lesson.Place;

                //Вызываем "Dispathcer":
                Dispatcher.Invoke(() =>
                {
                    ScheduleElement element = new ScheduleElement(number, name, teacher, place);

                    Schedule_LessonsList.Items.Add(element);
                });
            }
        }
        #endregion
    }
}
