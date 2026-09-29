namespace OmniMart.Application.Common;

public enum ErrorType
{
    Failure = 400,      
    Validation = 422,   
    NotFound = 404,     
    Conflict = 409,      
    Unauthorized = 401
}