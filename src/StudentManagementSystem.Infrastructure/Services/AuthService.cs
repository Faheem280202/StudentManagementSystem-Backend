using Microsoft.Data.SqlClient;
using StudentManagementSystem.Application.DTOs.Auth;
using StudentManagementSystem.Application.Interfaces;
using StudentManagementSystem.Infrastructure.Data;

namespace StudentManagementSystem.Infrastructure.Services;

public class AuthService(
    DbConnectionFactory connectionFactory,
    IUserRepository userRepository,
    IPasswordService passwordService,
    IJwtService jwtService) : IAuthService
{
    private readonly DbConnectionFactory _connectionFactory = connectionFactory;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordService _passwordService = passwordService;
    private readonly IJwtService _jwtService = jwtService;


    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await GetUserByEmailAsync(request.Email);

        if (user == null)
            return null;

        var isValid = _passwordService.VerifyPassword(request.Password, user.PasswordHash);

        if (!isValid)
            return null;

        var token = _jwtService.GenerateToken(user);

        return new LoginResponse
        {
            UserId = user.UserId,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            Token = token
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
                R.RoleName AS [Role],
                U.IsActive,
                U.CreatedOn,
                U.CreatedBy,
                U.ModifiedOn,
                U.ModifiedBy
            FROM tbl_Users AS U WITH(NOLOCK)
            INNER JOIN tbl_Roles AS R WITH(NOLOCK) ON R.Id = U.RoleId
            WHERE U.IsActive = 1
              AND R.IsActive = 1
              AND U.Email = @Email";

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

    private static LoginResponse MapUser(SqlDataReader reader)
    {
        return new LoginResponse
        {
            UserId = reader.GetInt32(reader.GetOrdinal("Id")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
            LastName = reader.GetString(reader.GetOrdinal("LastName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
            Role = reader.GetString(reader.GetOrdinal("Role")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
            CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetString(reader.GetOrdinal("CreatedBy")),
            ModifiedOn = reader.IsDBNull(reader.GetOrdinal("ModifiedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
            ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy"))
        };
    }
}

