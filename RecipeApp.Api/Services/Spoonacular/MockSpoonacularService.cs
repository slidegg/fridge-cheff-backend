namespace RecipeApp.Api.Services.Spoonacular;

public class MockSpoonacularService(ILogger<MockSpoonacularService> logger) : ISpoonacularService
{
    private static readonly List<SpoonacularRecipeSummary> MockSummaries =
    [
        new(716429, "Chicken Spinach Skillet",
            "https://img.spoonacular.com/recipes/716429-312x231.jpg",
            UsedIngredientCount: 3, MissedIngredientCount: 0,
            UsedIngredients:
            [
                new(1, "chicken breast", 200, "g"),
                new(2, "spinach", 100, "g"),
                new(3, "eggs", 2, "large"),
            ],
            MissedIngredients: [],
            Likes: 245),

        new(632660, "Broccoli Cheddar Omelette",
            "https://img.spoonacular.com/recipes/632660-312x231.jpg",
            UsedIngredientCount: 3, MissedIngredientCount: 0,
            UsedIngredients:
            [
                new(4, "eggs", 3, "large"),
                new(5, "broccoli", 80, "g"),
                new(6, "cheddar cheese", 30, "g"),
            ],
            MissedIngredients: [],
            Likes: 189),

        new(715769, "Greek Yogurt Chicken Bowl",
            "https://img.spoonacular.com/recipes/715769-312x231.jpg",
            UsedIngredientCount: 2, MissedIngredientCount: 0,
            UsedIngredients:
            [
                new(7, "chicken breast", 150, "g"),
                new(8, "greek yogurt", 100, "g"),
            ],
            MissedIngredients: [],
            Likes: 312),

        new(654959, "Spinach and Egg Scramble",
            "https://img.spoonacular.com/recipes/654959-312x231.jpg",
            UsedIngredientCount: 2, MissedIngredientCount: 0,
            UsedIngredients:
            [
                new(9, "spinach", 60, "g"),
                new(10, "eggs", 3, "large"),
            ],
            MissedIngredients: [],
            Likes: 156),

        new(642583, "Cheesy Broccoli Bake",
            "https://img.spoonacular.com/recipes/642583-312x231.jpg",
            UsedIngredientCount: 2, MissedIngredientCount: 0,
            UsedIngredients:
            [
                new(11, "broccoli", 200, "g"),
                new(12, "cheddar cheese", 60, "g"),
            ],
            MissedIngredients: [],
            Likes: 201),
    ];

    private static readonly SpoonacularRecipeDetail MockDetail = new(
        Id: 716429,
        Title: "Chicken Spinach Skillet",
        Image: "https://img.spoonacular.com/recipes/716429-556x370.jpg",
        Servings: 2,
        ReadyInMinutes: 25,
        SourceUrl: null,
        ExtendedIngredients:
        [
            new("chicken breast", 400, "g"),
            new("spinach", 100, "g"),
            new("eggs", 2, "large"),
            new("olive oil", 1, "tbsp"),
            new("garlic", 2, "cloves"),
            new("salt", 1, "tsp"),
            new("black pepper", 0.5, "tsp"),
        ],
        Steps:
        [
            new(1, "Heat olive oil in a large skillet over medium-high heat."),
            new(2, "Season chicken breast with salt and pepper. Cook for 6-7 minutes per side until golden and cooked through."),
            new(3, "Remove chicken and let it rest on a cutting board."),
            new(4, "Add minced garlic to the same pan and cook for 1 minute until fragrant."),
            new(5, "Add spinach and cook until wilted, about 2 minutes."),
            new(6, "Slice chicken and return to the pan. Push everything to the sides."),
            new(7, "Crack eggs into the center and cook to your preference (over easy recommended)."),
            new(8, "Season to taste and serve immediately."),
        ],
        Nutrition: new SpoonacularNutrition(
        [
            new("Calories", 520, "kcal"),
            new("Protein", 48, "g"),
            new("Carbohydrates", 8, "g"),
            new("Fat", 32, "g"),
            new("Fiber", 3, "g"),
        ])
    );

    public Task<List<SpoonacularRecipeSummary>> FindByIngredientsAsync(
        IEnumerable<string> ingredients, int number, int ranking, bool ignorePantry)
    {
        logger.LogInformation("Mock: finding recipes for ingredients [{Ingredients}]",
            string.Join(", ", ingredients));

        return Task.FromResult(MockSummaries.Take(number).ToList());
    }

    public Task<SpoonacularRecipeDetail?> GetRecipeDetailAsync(int recipeId)
    {
        logger.LogInformation("Mock: getting detail for recipe {RecipeId}", recipeId);

        // Return mock detail with the requested id substituted
        var detail = MockDetail with { Id = recipeId };
        return Task.FromResult<SpoonacularRecipeDetail?>(detail);
    }
}
