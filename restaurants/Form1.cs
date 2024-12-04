using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace restaurants
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Load += new EventHandler(Form1_Load);

            textBoxLogin.Text = "Введите логин";
            textBoxLogin.ForeColor = Color.Gray;

            textBoxPassword.Text = "Введите пароль";
            textBoxPassword.ForeColor = Color.Gray;
            textBoxPassword.UseSystemPasswordChar = false;

            // Подключение событий для текстовых полей
            textBoxLogin.Enter += textBoxLogin_Enter;
            textBoxLogin.Leave += textBoxLogin_Leave;
            textBoxPassword.Enter += textBoxPassword_Enter;
            textBoxPassword.Leave += textBoxPassword_Leave;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Проверка соединения с базой данных при запуске формы
            DatabaseHelper.CheckDatabaseConnection();
        }

        // Остальной код для обработки текстовых полей
        private void textBoxLogin_Enter(object sender, EventArgs e)
        {
            if (textBoxLogin.Text == "Введите логин")
            {
                textBoxLogin.Text = "";
                textBoxLogin.ForeColor = Color.Black;
            }
        }

        private void textBoxLogin_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxLogin.Text))
            {
                textBoxLogin.Text = "Введите логин";
                textBoxLogin.ForeColor = Color.Gray;
            }
        }

        private void textBoxPassword_Enter(object sender, EventArgs e)
        {
            if (textBoxPassword.Text == "Введите пароль")
            {
                textBoxPassword.Text = "";
                textBoxPassword.ForeColor = Color.Black;
                textBoxPassword.UseSystemPasswordChar = true;
            }
        }

        private void textBoxPassword_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxPassword.Text))
            {
                textBoxPassword.UseSystemPasswordChar = false;
                textBoxPassword.Text = "Введите пароль";
                textBoxPassword.ForeColor = Color.Gray;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string login = textBoxLogin.Text;
            string password = textBoxPassword.Text;

            bool tableExists = DatabaseHelper.CheckIfUsersTableExists();
            if (tableExists)
            {
                // Если таблица существует, продолжайте работу
                // Ваш код для проверки логина и пароля
                if (UserAuthenticator.Authenticate(login, password))
                {
                    MessageBox.Show("Успешный вход!");
                    // Здесь можно выполнить действия после успешного входа (например, открыть другую форму)
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль!");
                }
            }
            else
            {
                // Если таблица не существует, уведомите пользователя
                MessageBox.Show("Таблица Users не найдена в базе данных.");
            }
        }
    }
}
