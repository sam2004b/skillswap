namespace Backend.DTOs.Reviews;

public class TeacherReviewsResponse
{
    public string TeacherId { get; set; } = string.Empty;

    public string TeacherName { get; set; } = string.Empty;

    public double AverageRating { get; set; }

    public int TotalReviews { get; set; }

    public List<ReviewResponse> Reviews { get; set; } = new();
}