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

            bool access = UserAuthenticator.Authenticate(login, password);
            if (access)
            {
                int userId = UserAuthenticator.GetUserId(login, password);
                if (userId != -1)
                {
                    MainWindow main = new MainWindow(userId);
                    main.Show();
                    this.Hide();
                }
            }
           
            else
            {
                MessageBox.Show("Неверный логин или пароль!");
            }
        }
    }
}
