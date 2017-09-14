using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace BloodPressure
{
    public partial class Main : Form
    {
        SqlConnection connection = new SqlConnection("Data Source=CODECOOL\\PETER;Initial Catalog=Blood;Integrated Security=True");

        public Main()
        {
            InitializeComponent();
        }


        private Boolean Verify(string savedPasswordHash, string password)
        {
            byte[] hashBytes = Convert.FromBase64String(savedPasswordHash);
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);
            for (int i = 0; i < 20; i++)
                if (hashBytes[i + 16] != hash[i])
                    return false;
            return true;
        }

        private string Hash(string password)
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);
            var pbkdf2 = new Rfc2898DeriveBytes(PasswordTextBox.Text, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);
            string savedPasswordHash = Convert.ToBase64String(hashBytes);
            return savedPasswordHash;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            connection.Close();
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            
            string sql = "SELECT password FROM[Blood].[dbo].[User] u " +
                "INNER JOIN [Blood].[dbo].[Password] p on u.PasswordId = p.Id WHERE u.LoginName = @LoginName";

            try
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@LoginName", UserTextBox.Text);
                SqlDataReader dataReader = command.ExecuteReader();
                if (dataReader.HasRows)
                {
                    while (dataReader.Read())
                    {
                        if (Verify(dataReader.GetString(0), PasswordTextBox.Text))
                        {
                            MessageBox.Show("Succesfully logged in!");
                        } else
                        {
                            MessageBox.Show("Invalid user name or password!");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Invalid user name or password!");
                }
                dataReader.Close();
                command.Dispose();
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }        
        }

        private void RegisterButton_Click(object sender, EventArgs e)
        {
            if (PasswordTextBox.Text.Equals(RePasswordTextBox.Text) && !PasswordTextBox.Text.Equals("") && !UserTextBox.Text.Equals(""))
            {  
                int PasswordId;
                bool IsNewUser = CheckUserName();
                string HashedPassword = Hash(PasswordTextBox.Text);

                if (IsNewUser) {
                    PasswordId = GetPasswordId();
                    if ( SavePassword(PasswordId,HashedPassword) && SaveNewUser(PasswordId))
                    {
                        MessageBox.Show("User: " + UserTextBox.Text + " succesfully created!");
                        UserTextBox.Clear();
                        PasswordTextBox.Clear();
                        RePasswordTextBox.Clear();
                    }
                } 
            }
            else MessageBox.Show("The passwords/user are differents or empty!");
        }

        private bool SaveNewUser(int PasswordId)
        {
            int? UserId = null;
            bool result = false;

            try
            {
                connection.Open();
                string sql = "Select Max(Id) from [Blood].[dbo].[User]";
                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader dataReader = command.ExecuteReader();
                if (dataReader.HasRows)
                {
                    while (dataReader.Read())
                    {
                        UserId = dataReader.GetInt32(0);
                    }  
                } else
                {
                    MessageBox.Show("User table is empty");
                }
                
                dataReader.Close();
                
                sql = "Insert into [Blood].[dbo].[User] (Id,LoginName,PasswordId) Values (@Id,@LoginName,@PasswordId)";
                try
                {
                    command = new SqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@Id", UserId + 1);
                    command.Parameters.AddWithValue("@LoginName", UserTextBox.Text);
                    command.Parameters.AddWithValue("@PasswordId", PasswordId + 1);
                    command.ExecuteNonQuery();
                    result = true;
                } catch (Exception ex)
                {
                    Console.WriteLine("{0} Exception caught.", ex);
                }
                command.Dispose();
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
            return result;
        }

        private bool CheckUserName()
        {
            bool result = false;
            string sql = "Select LoginName from [Blood].[dbo].[User] where LoginName = @LoginName";
            
            try
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@LoginName", UserTextBox.Text);
                SqlDataReader dataReader = command.ExecuteReader();

                if (dataReader.HasRows)
                {
                    MessageBox.Show("The user name is already exists!");
                    result = false;
                } else
                {
                    result = true;
                }

                dataReader.Close();
                command.Dispose();
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
            return result;
        }

        private int GetPasswordId()
        {
            int PasswordId = 0;
            string sql = "Select Max(Id) from [Blood].[dbo].[Password]";
            
            try
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sql, connection);
                SqlDataReader dataReader = command.ExecuteReader();
                try
                {
                    if (dataReader.HasRows)
                    {
                        while (dataReader.Read())
                        {
                            PasswordId = dataReader.GetInt32(0);
                        }
                     }
                    else
                    {
                        MessageBox.Show("User table is empty");
                    }
                } catch
                {
                }
                dataReader.Close();
                command.Dispose();
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
            return PasswordId;
        }

        private bool SavePassword(int PasswordId, string HashedPassword)
        {
            bool result = false;
            string sql = "Insert into [Blood].[dbo].[Password] (Id,password) Values (@Id,@HashedPassword)";

            try
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@HashedPassword", HashedPassword);
                command.Parameters.AddWithValue("@Id", PasswordId+1);
                command.ExecuteNonQuery();
                result = true;
                command.Dispose();
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
            return result;
        }
    }
}
