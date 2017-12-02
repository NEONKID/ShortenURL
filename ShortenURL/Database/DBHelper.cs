using System;
using System.Text;

using MySql.Data.MySqlClient;

namespace ShortenURL
{
    /// <summary>
    /// MySQL / MariaDB 를 사용하여, URL를 저장합니다.
    /// 테스트는 MariaDB로 진행하였습니다.
    /// Created by neonkid 12/01/17
    /// </summary>
    public class DBHelper
    {
        private string strConn;
        private string table_Name;

        /// <summary>
        /// MySQL DBMS 를 사용하여, URL DB 연결
        /// DB는 utf8 인코딩에 InnoDB 엔진을 사용하였으며,
        /// table 형식은 id(INT, AUTO_INCREMENT, PRIMARY_KEY) / URL (VARCHAR) 로 되어있습니다.
        /// </summary>
        /// <param name="serverAddress">서버 주소</param>
        /// <param name="db">DB 이름</param>
        /// <param name="tableName">테이블 이름</param>
        /// <param name="userId">사용자 ID</param>
        /// <param name="userPwd">사용자 비밀번호</param>
        public DBHelper(String serverAddress, String db, String tableName, String userId, String userPwd)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Server=" + serverAddress + ";");
            builder.Append("Database=" + db + ";");
            builder.Append("Uid=" + userId + ";");
            builder.Append("Pwd=" + userPwd + ";");

            strConn = builder.ToString();
            this.table_Name = tableName;
        }

        /// <summary>
        /// DB 테이블에 URL을 삽입합니다.
        /// </summary>
        /// <param name="value">삽입할 URL</param>
        public void Insert(String value)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(strConn))
                {
                    connection.Open();
                    MySqlCommand cmd = new MySqlCommand("INSERT INTO " + table_Name + " VALUES (NULL, \"" + value + "\" )", connection);
                    cmd.ExecuteNonQuery();
                }
            }
            catch(MySqlException)
            {
                return;
            }
        }

        /// <summary>
        /// URL 값을 가지고, ID를 가져옵니다.
        /// </summary>
        /// <param name="value">ID 값</param>
        /// <returns></returns>
        public String getIDforURL(String value)
        {
            string rtnID = null;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(strConn))
                {
                    connection.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT id from " + table_Name + " where URL = \"" + value + "\"", connection);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                        rtnID = reader["id"].ToString();
                    reader.Close();
                }
            }
            catch (MySqlException)
            {
                return null;
            }
            return rtnID;
        }

        /// <summary>
        /// ID 값으로 ID를 가져옵니다.
        /// ID 값의 유무를 확인하는 용도로 사용합니다.
        /// </summary>
        /// <param name="value">ID 값</param>
        /// <returns></returns>
        public String getID(String value)
        {
            string rtnID = null;
            using (MySqlConnection connection = new MySqlConnection(strConn))
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT id from " + table_Name + " where id = \"" + value + "\"", connection);
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                    rtnID = reader["id"].ToString();
                reader.Close();
            }
            return rtnID;
        }

        /// <summary>
        /// ID 값으로 URL을 가져옵니다.
        /// 리다이렉팅 용도로 사용됩니다.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public String getURLforID(String value)
        {
            string rtnURL = null;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(strConn))
                {
                    connection.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT URL from " + table_Name + " where id = \"" + value + "\"", connection);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                        rtnURL = reader["URL"].ToString();
                    reader.Close();
                }
            }
            catch (MySqlException)
            {
                return null;
            }
            return rtnURL;
        }

        /// <summary>
        /// URL 주소로 URL을 가져옵니다.
        /// DBMS 에 URL 주소 존재 여부를 확인할 떄 사용합니다.
        /// </summary>
        /// <param name="value">URL 주소</param>
        /// <returns></returns>
        public String getURL(String value)
        {
            string rtnURL = null;
            try
            {

            }
            catch(MySqlException)
            {
                return null;
            }
            return rtnURL;
        }
    }
}
