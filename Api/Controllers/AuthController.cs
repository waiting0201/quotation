using Microsoft.AspNetCore.Mvc;
using QuotationApi.DTOs.Auth;
using QuotationApi.Helpers;
using QuotationApi.Router;
using QuotationApi.Services;

namespace QuotationApi.Controllers;

/// <summary>
/// 認證相關端點
/// POST auth/login - 登入，取得 JWT token
/// GET  auth/me    - 取得目前登入的使用者資訊
/// </summary>
public class AuthController
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// POST auth/login
    /// Body: { email, password }
    /// 成功回傳 { data: { token, user } }
    /// </summary>
    public async Task<IActionResult> Login(RouteContext context)
    {
        LoginRequest request;
        try
        {
            request = await context.ReadBodyAsync<LoginRequest>();
        }
        catch (ArgumentException ex)
        {
            return context.BadRequest(ex.Message);
        }

        // 基本欄位驗證（避免空白字串進入 service 層）
        if (string.IsNullOrWhiteSpace(request.Email))
            return context.BadRequest("Email is required.");
        if (string.IsNullOrWhiteSpace(request.Password))
            return context.BadRequest("Password is required.");

        var result = await _authService.LoginAsync(request.Email.Trim(), request.Password);

        if (result == null)
        {
            // 刻意不區分「帳號不存在」與「密碼錯誤」，防止帳號列舉攻擊
            return context.Unauthorized("Invalid email or password.");
        }

        return context.Ok(result);
    }

    /// <summary>
    /// GET auth/me
    /// 需要有效 JWT（由 JwtAuthMiddleware 驗證）
    /// 回傳目前登入使用者的完整資訊（含最新權限）
    /// </summary>
    public async Task<IActionResult> Me(RouteContext context)
    {
        if (context.CurrentUser == null)
            return context.Unauthorized();

        var userIdStr = JwtHelper.GetUserId(context.CurrentUser);
        if (!Guid.TryParse(userIdStr, out var userId))
            return context.Unauthorized("Invalid token claims.");

        var userInfo = await _authService.GetUserInfoAsync(userId);
        if (userInfo == null)
            return context.NotFound("User not found.");

        return context.Ok(userInfo);
    }
}
