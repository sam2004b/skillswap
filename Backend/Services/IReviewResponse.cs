using Backend.DTOs.Reviews;

namespace Backend.Services;

public interface IReviewService
{
    Task<ReviewResponse> CreateReviewAsync(
        string reviewerId,
        CreateReviewRequest request);

    Task<TeacherReviewsResponse> GetTeacherReviewsAsync(
        string teacherId);

    Task<List<ReviewResponse>> GetMyReviewsAsync(
        string reviewerId);

    Task<ReviewResponse?> GetReviewByIdAsync(int id);

    Task DeleteReviewAsync(int id, string reviewerId);
}