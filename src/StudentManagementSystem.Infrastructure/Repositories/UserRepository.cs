using Microsoft.Data.SqlClient;
using StudentManagementSystem.Application.Interfaces;
using StudentManagementSystem.Domain.Entities;
using StudentManagementSystem.Infrastructure.Data;
using StudentManagementSystem.Domain.Entities.Response;

namespace StudentManagementSystem.Infrastructure.Repositories
{
    public class UserRepository(DbConnectionFactory connectionFactory) : IUserRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        private static UserResponse MapUser(SqlDataReader reader)
        {
            var rolesString = reader["Roles"]?.ToString();

            return new UserResponse
            {
                UserId = Convert.ToInt32(reader["Id"]),
                UserName = reader["UserName"].ToString()!,
                FirstName = reader["FirstName"].ToString()!,
                LastName = reader["LastName"].ToString()!,
                Email = reader["Email"].ToString()!,

                Roles = reader["Roles"] == DBNull.Value
                    ? new List<string>()
                    : reader["Roles"].ToString()!
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => r.Trim())
                        .ToList(),

                            IsActive = Convert.ToBoolean(reader["IsActive"]),
                            CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),

                            CreatedBy = reader["CreatedBy"] == DBNull.Value
                    ? null
                    : reader["CreatedBy"].ToString(),

                            ModifiedDate = reader["ModifiedDate"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(reader["ModifiedDate"]),

                            ModifiedBy = reader["ModifiedBy"] == DBNull.Value
                    ? null
                    : reader["ModifiedBy"].ToString()
            };
        }

        public async Task<List<UserResponse>> GetAllUsersAsync()
        {
            var users = new List<UserResponse>();

            using var connection = _connectionFactory.CreateConnection();

            var query = UserQuery + @"
                GROUP BY
                    U.Id,
                    U.UserName,
                    U.FirstName,
                    U.LastName,
                    U.Email,
                    U.IsActive,
                    U.CreatedDate,
                    U.CreatedBy,
                    U.ModifiedDate,
                    U.ModifiedBy";

            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }

            return users;
        }

        public async Task<UserResponse?> GetByIdAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();

            var query = UserQuery + @"
                AND U.Id = @Id
                GROUP BY
                    U.Id,
                    U.UserName,
                    U.FirstName,
                    U.LastName,
                    U.Email,
                    U.IsActive,
                    U.CreatedDate,
                    U.CreatedBy,
                    U.ModifiedDate,
                    U.ModifiedBy";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapUser(reader);
            }

            return null;
        }

        public async Task<int> CreateUserAsync(User user)
        {
            using var connection = _connectionFactory.CreateConnection();

            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                const string query = @"
                    DECLARE @UserId INT;

                    INSERT INTO tbl_Users
                    (
                        UserName,
                        FirstName,
                        LastName,
                        Email,
                        PasswordHash,
                        CreatedBy
                    )
                    VALUES
                    (
                        @UserName,
                        @FirstName,
                        @LastName,
                        @Email,
                        @PasswordHash,
                        @CreatedBy
                    );

                    SET @UserId = SCOPE_IDENTITY();

                    INSERT INTO tbl_UserRoles
                    (
                        UserId,
                        RoleId,
                        CreatedBy
                    )
                    VALUES
                    (
                        @UserId,
                        @RoleId,
                        @CreatedBy
                    );

                    SELECT @UserId;
                ";

                using var command = new SqlCommand(query, connection, transaction);

                command.Parameters.AddWithValue("@UserName", user.UserName);
                command.Parameters.AddWithValue("@FirstName", user.FirstName);
                command.Parameters.AddWithValue("@LastName", user.LastName);
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                command.Parameters.AddWithValue("@CreatedBy", user.CreatedBy);
                command.Parameters.AddWithValue("@RoleId", user.RoleId);

                var result = await command.ExecuteScalarAsync();

                await transaction.CommitAsync();

                return Convert.ToInt32(result);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        /* Common queries */
        private const string UserQuery = @"
            SELECT
                U.Id,
                U.UserName,
                U.FirstName,
                U.LastName,
                U.Email,
                STRING_AGG(R.RoleName, ', ') AS Roles,
                U.IsActive,
                U.CreatedDate,
                U.CreatedBy,
                U.ModifiedDate,
                U.ModifiedBy
            FROM tbl_Users AS U WITH(NOLOCK)
            INNER JOIN tbl_UserRoles AS UR WITH(NOLOCK) ON UR.UserId = U.Id
            INNER JOIN tbl_Roles AS R WITH(NOLOCK) ON R.Id = UR.RoleId
            WHERE U.IsActive = 1
              AND UR.IsActive = 1
              AND R.IsActive = 1";
    }
}
