namespace RecipeApp.Api.Exceptions;

public class RateLimitException(string message) : Exception(message);
