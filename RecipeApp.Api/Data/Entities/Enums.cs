namespace RecipeApp.Api.Data.Entities;

public enum ScanStatus
{
    Pending,
    Processed,
    Failed
}

public enum RecipeGoal
{
    ProteinFirst,
    LowCalories,
    TastyFirst
}

public enum InteractionAction
{
    Viewed,
    Saved
}

public enum ApiProvider
{
    OpenAI,
    Spoonacular
}

public enum LimitType
{
    Scan,
    RecipeSearch,
    RecipeDetail
}
