using CrossCutting.Enums;
using CrossCutting.Guarder;
using CrossCutting.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebAPI.ViewModels;

namespace WebAPI.Attributes;

/// <summary>
/// 套用基本回傳物件與處理異常的回拋
/// </summary>
public class ApiResponseFilterAttribute : ActionFilterAttribute, IExceptionFilter
{
    private readonly ILogger<ApiResponseFilterAttribute> _logger;
    
    public ApiResponseFilterAttribute(ILogger<ApiResponseFilterAttribute> logger)
    {
        this._logger = logger;
    }
    
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult result)
        {
            result.Value = ApiResponse<object>.Success(result.Value);
        }
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var resp = await next();
        if (resp.Result is ObjectResult result)
        {
            result.Value = ApiResponse<object>.Success(result.Value);
        }
    }

    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;

        if (exception is BadHttpRequestException badRequest)
        {
            context.HttpContext.Response.StatusCode = badRequest.StatusCode;
            context.Result = new ObjectResult(badRequest.Message);
        }
        else
        {
            context.HttpContext.Response.StatusCode = 200;
            context.Result = new ObjectResult(Parse(exception));
        }
        context.ExceptionHandled = true; // IMPORTANT
    }
    
    private static ApiResponse<object?> Parse(Exception ex)
    {
        var guarderEx = ExceptionHelper.Find<GuarderException>(ex);
        if (guarderEx != null)
        {
            var otherwiseCode = EnumHelper.CastTo<ActionCodeEnum>(guarderEx.Code);
            if (otherwiseCode != null)
            {
                return ApiResponse<object?>.Otherwise(otherwiseCode, guarderEx.Message, guarderEx.ExtraData);
            }

            var failureCode = EnumHelper.CastTo<ErrorCodeEnum>(guarderEx.Code);
            if (failureCode != null)
            {
                return ApiResponse<object?>.Failure(failureCode, guarderEx.Message);
            }
        }

        return ApiResponse<object?>.Failure(null, ex.Message);
    }
}