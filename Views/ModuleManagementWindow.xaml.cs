using Cursach.BLL;
using Cursach.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Cursach
{
    public partial class ModuleManagementWindow : Window
    {
        private readonly int _courseId;
        private readonly string _courseTitle;
        private readonly IModuleService _moduleService;
        private readonly IUserService _userService;

        public ModuleManagementWindow(int courseId, string courseTitle, IModuleService moduleService, IUserService userService)
        {
            InitializeComponent();
            _courseId = courseId;
            _courseTitle = courseTitle;
            _moduleService = moduleService;
            _userService = userService;
            this.Title = $"Управление модулями - {_courseTitle}";
            lblCourseTitle.Text = $"Модули курса: {_courseTitle}";
            LoadModules();
            LoadEnrolledUsers();
        }

        private void LoadModules()
        {
            try
            {
                dgModules.ItemsSource = _moduleService.GetModulesByCourseId(_courseId);
            }
            catch (Exception ex)
            {
                LogError(ex, "загрузке модулей");
                string errorMessage = "Ошибка загрузки модулей: " + ex.Message;
                if (ex.InnerException != null)
                    errorMessage += "\n\nДетали: " + ex.InnerException.Message;
                MessageBox.Show(errorMessage, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEnrolledUsers()
        {
            try
            {
                List<User> enrolledUsers = new List<User>();
                using (MySqlConnection conn = new MySqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(
                        "SELECT u.UserID, u.UserName, u.UserType " +
                        "FROM user u " +
                        "JOIN enrollment e ON u.UserID = e.UserID " +
                        "WHERE e.CourseID = @courseId", conn);
                    cmd.Parameters.AddWithValue("@courseId", _courseId);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            enrolledUsers.Add(new User
                            {
                                UserID = reader.GetInt32("UserID"),
                                UserName = reader.GetString("UserName"),
                                UserType = reader.GetString("UserType")
                            });
                        }
                    }
                }
                dgUsers.ItemsSource = enrolledUsers;
            }
            catch (Exception ex)
            {
                LogError(ex, "загрузке пользователей");
                string errorMessage = "Ошибка загрузки пользователей: " + ex.Message;
                if (ex.InnerException != null)
                    errorMessage += "\n\nДетали: " + ex.InnerException.Message;
                MessageBox.Show(errorMessage, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddModule_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtModuleTitle.Text))
            {
                MessageBox.Show("Пожалуйста, введите название модуля.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtModuleTitle.Focus();
                return;
            }

            try
            {
                _moduleService.AddModule(_courseId, txtModuleTitle.Text, txtModuleContent.Text);
                txtModuleTitle.Clear();
                txtModuleContent.Clear();
                LoadModules();
                MessageBox.Show("Модуль успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogError(ex, "добавлении модуля");
                string errorMessage = "Ошибка при добавлении модуля: " + ex.Message;
                if (ex.InnerException != null)
                    errorMessage += "\n\nДетали: " + ex.InnerException.Message;
                MessageBox.Show(errorMessage, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteModule_Click(object sender, RoutedEventArgs e)
        {
            if (dgModules.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите модуль для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedModule = (Module)dgModules.SelectedItem;
            MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить модуль '{selectedModule.Title}' (ID: {selectedModule.ModuleID})?",
                                                     "Подтверждение удаления",
                                                     MessageBoxButton.YesNo,
                                                     MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _moduleService.DeleteModule(selectedModule.ModuleID);
                LoadModules();
                LoadEnrolledUsers(); // Обновляем список пользователей после удаления модуля
                MessageBox.Show("Модуль успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogError(ex, "удалении модуля");
                string errorMessage = "Ошибка при удалении модуля: " + ex.Message;
                if (ex.InnerException is MySql.Data.MySqlClient.MySqlException mysqlEx && mysqlEx.Number == 1451)
                    errorMessage += "\n\nВозможно, этот модуль используется в записях о прохождении курсов и не может быть удален.";
                else if (ex.InnerException != null)
                    errorMessage += "\n\nДетали: " + ex.InnerException.Message;
                MessageBox.Show(errorMessage, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogError(Exception ex, string action)
        {
            string logMessage = $"{DateTime.Now}: Ошибка при {action}: {ex.Message}";
            if (ex.InnerException != null)
                logMessage += $"\nДетали: {ex.InnerException.Message}";
            System.IO.File.AppendAllText("error.log", logMessage + Environment.NewLine);
        }
    }
}