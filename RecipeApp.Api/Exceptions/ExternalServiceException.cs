namespace RecipeApp.Api.Exceptions;

/// <summary>Thrown when an upstream API (Spoonacular, OpenAI) fails or is unavailable — not the caller's fault.</summary>
public class ExternalServiceException(string message) : Exception(message);
