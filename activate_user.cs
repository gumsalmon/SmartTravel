using Microsoft.Data.SqlClient;
using BCrypt.Net;

string connectionString = "Server=.\\SQLEXPRESS;Database=VinhKhanhTourDB;Trusted_Connection=True;TrustServerCertificate=True";
string phone = "0333333333"; // Số điện thoại khách vừa thanh toán
string fullName = "Test3";
string stallName = "Ốc Test3";
string password = "123456";
double lat = 10.7618;
double lng = 106.704;

using (var conn = new SqlConnection(connectionString))
{
    conn.Open();
    using (var trans = conn.BeginTransaction())
    {
        try
        {
            // 1. Tạo User
            string hash = BCrypt.Net.BCrypt.HashPassword(password);
            string insertUser = "INSERT INTO Users (username, password_hash, full_name, role, is_deleted) OUTPUT INSERTED.Id VALUES (@u, @p, @f, @r, 0)";
            int userId;
            using (var cmd = new SqlCommand(insertUser, conn, trans))
            {
                cmd.Parameters.AddWithValue("@u", phone);
                cmd.Parameters.AddWithValue("@p", hash);
                cmd.Parameters.AddWithValue("@f", fullName);
                cmd.Parameters.AddWithValue("@r", "StallOwner");
                userId = (int)cmd.ExecuteScalar();
            }

            // 2. Tạo Stall
            string insertStall = "INSERT INTO Stalls (owner_id, name_default, latitude, longitude, is_open, sort_order, is_deleted) OUTPUT INSERTED.Id VALUES (@oid, @n, @lat, @lng, 1, 0, 0)";
            int stallId;
            using (var cmd = new SqlCommand(insertStall, conn, trans))
            {
                cmd.Parameters.AddWithValue("@oid", userId);
                cmd.Parameters.AddWithValue("@n", stallName);
                cmd.Parameters.AddWithValue("@lat", lat);
                cmd.Parameters.AddWithValue("@lng", lng);
                stallId = (int)cmd.ExecuteScalar();
            }

            // 3. Tạo Subscription
            string insertSub = "INSERT INTO Subscriptions (stall_id, device_id, is_active, start_date, expiry_date, activation_code) VALUES (@sid, @did, 1, GETDATE(), DATEADD(day, 30, GETDATE()), @ac)";
            using (var cmd = new SqlCommand(insertSub, conn, trans))
            {
                cmd.Parameters.AddWithValue("@sid", stallId);
                cmd.Parameters.AddWithValue("@did", "DEV-" + stallId);
                cmd.Parameters.AddWithValue("@ac", "ACT-MANUAL-" + Guid.NewGuid().ToString().Substring(0, 4));
                cmd.ExecuteNonQuery();
            }

            trans.Commit();
            Console.WriteLine($"SUCCESS: Created User {userId} and Stall {stallId} for phone {phone}");
        }
        catch (Exception ex)
        {
            trans.Rollback();
            Console.WriteLine("ERROR: " + ex.Message);
        }
    }
}
