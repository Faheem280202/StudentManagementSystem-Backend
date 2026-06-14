using Microsoft.Data.SqlClient;
using StudentManagementSystem.Application.DTOs.Auth;
using StudentManagementSystem.Application.Interfaces;
using StudentManagementSystem.Infrastructure.Data;

namespace StudentManagementSystem.Infrastructure.Services;

public class AuthService(
    DbConnectionFactory connectionFactory,
    IUserRepository userRepository,
    IPasswordService passwordService) : IAuthService
{
    private readonly DbConnectionFactory _connectionFactory = connectionFactory;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordService _passwordService = passwordService;

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await GetUserByEmailAsync(request.Email);

        if (user == null)
            return null;

        var isValid = _passwordService.VerifyPassword(request.Password, user.PasswordHash);

        if (!isValid)
            return null;

        return new LoginResponse
        {
            UserId = user.UserId,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Roles = user.Roles,
            IsActive = user.IsActive,
            CreatedDate = user.CreatedDate,
            CreatedBy = user.CreatedBy,
            ModifiedDate = user.ModifiedDate,
            ModifiedBy = user.ModifiedBy
        };
    }

    public async Task<LoginResponse?> GetUserByEmailAsync(string email)
    {
        using var connection = _connectionFactory.CreateConnection();

        var query = @"
            SELECT
                U.Id,
                U.UserName,
                U.FirstName,
                U.LastName,
                U.Email,
                U.PasswordHash,
                STRING_AGG(R.RoleName, ',') AS Roles,
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
              AND R.IsActive = 1
              AND U.Email = @Email
            GROUP BY
                U.Id,
                U.UserName,
                U.FirstName,
                U.LastName,
                U.Email,
                U.PasswordHash,
                U.IsActive,
                U.CreatedDate,
                U.CreatedBy,
                U.ModifiedDate,
                U.ModifiedBy";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Email", email);

        await connection.OpenAsync();

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }

        return null;
    }

    private LoginResponse MapUser(SqlDataReader reader)
    {
        return new LoginResponse
        {
            UserId = reader.GetInt32(reader.GetOrdinal("Id")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
            LastName = reader.GetString(reader.GetOrdinal("LastName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
            Roles = [.. reader.GetString(reader.GetOrdinal("Roles")).Split(',', StringSplitOptions.RemoveEmptyEntries)],
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
            CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetString(reader.GetOrdinal("CreatedBy")),
            ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
            ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy"))
        };
    }
}

