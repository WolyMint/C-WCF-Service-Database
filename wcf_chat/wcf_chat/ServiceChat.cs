using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;



namespace wcf_chat
{
  
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ServiceChat : IServiceChat
    {
        public static string connectionString = "Server = .; Database = Chat; Trusted_Connection = True; TrustServerCertificate = True;";
        List<ServerUser> users = new List<ServerUser>();
        int nextId = 10;

        public int NewID()
        {
            int newId;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string sqlExpression = "select MAX(ID) as MaxID from ChatUsers";
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                object result =command.ExecuteScalar();
                if (result != null) { newId = (int)result + 1; }
                else { newId = 1; }
                command.ExecuteNonQuery();
            }
            return newId;
        }

        public int Connect(string name)
        {
            if (name == "")
            {
                throw new FaultException<NoDataFault>(new NoDataFault(), "Введите имя!");
            }
            if (name.Length > 50)
            {
                throw new FaultException<TooLongName>(new TooLongName(), "Слишком длинное имя");
            }
            ServerUser user = new ServerUser() {
                ID = NewID(),
                Name = name,
                Status = "Connected",
                operationContext = OperationContext.Current
            };
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string sqlExpression = "Insert into ChatUsers (Name, ID, Status) Values (@name, @id, @status)";
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                command.Parameters.AddWithValue("@id", user.ID);
                command.Parameters.AddWithValue("@name", user.Name);
                command.Parameters.AddWithValue("@status", user.Status);
                command.ExecuteNonQuery();
            }
            //nextId++;

            SendMsg(": "+user.Name+" подключился к чату!",0);
            users.Add(user);
            return user.ID;

        }

        public void Disconnect(int id)
        {
            var user = users.FirstOrDefault(i => i.ID == id);
            if (user != null)
            {
                string status = "Disconnected";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sqlExpression = "Update ChatUsers set Status = @status  where ID = @id";
                    SqlCommand command = new SqlCommand(sqlExpression, connection);
                    command.Parameters.AddWithValue("@id", user.ID);
                    command.Parameters.AddWithValue("@status", status);
                    command.ExecuteNonQuery();
                }

                SendMsg(": " + user.Name + " покинул чат!", 0);
            }
            
        }

        public void SendMsg(string msg, int id)
        {
            foreach (var item in users)
            {
                string answer = DateTime.Now.ToShortTimeString();

                var user = users.FirstOrDefault(i => i.ID == id);
                if (user != null)
                {
                    answer += ": " + user.Name+" ";
                }
                
                answer += msg;
                item.operationContext.GetCallbackChannel<IServerChatCallback>().MsgCallback(answer);
            }
        }
    }
}
