
using Backend.DTOs;

namespace Backend.Services;

public interface IReviewService
{
    Task<ReviewResponse> CreateAsync(
        CreateReviewRequest request, string reviewerId);

    Task<List<ReviewResponse>> GetTeacherReviewsAsync(
        string teacherId);

    Task<List<ReviewResponse>> GetMyReviewsAsync(
        string reviewerId);

    Task<ReviewResponse?> UpdateAsync(
        int id, UpdateReviewRequest request, string reviewerId);

    Task<bool> DeleteAsync(int id, string reviewerId);

    Task<double?> GetAverageRatingAsync(string teacherId);
}
