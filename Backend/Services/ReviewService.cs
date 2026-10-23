
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _context;

    public ReviewService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewResponse> CreateAsync(
        CreateReviewRequest request, string reviewerId)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BookingId);

        if (booking == null)
            throw new ArgumentException("Booking not found.");

        if (booking.LearnerId != reviewerId)
            throw new ArgumentException(
                "Only the learner who made the booking can review the teacher.");

        if (booking.Status != "Completed")
            throw new ArgumentException(
                "You can only review a completed booking.");

        if (await _context.Reviews.AnyAsync(
            r => r.BookingId == request.BookingId))
        {
            throw new ArgumentException(
                "This booking already has a review.");
        }

        var review = new Review
        {
            BookingId = booking.Id,
            ReviewerId = reviewerId,
            TeacherId = booking.TeacherId,
            Rating = request.Rating,
            Comment = request.Comment?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return await BuildResponseAsync(review.Id);
    }

    public async Task<List<ReviewResponse>> GetTeacherReviewsAsync(
        string teacherId)
    {
        return await _context.Reviews
            .AsNoTracking()
            .Where(r => r.TeacherId == teacherId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new ReviewResponse
            {
                Id = r.Id,
                BookingId = r.BookingId,
                ReviewerId = r.ReviewerId,
                ReviewerName = r.Reviewer != null
                    ? r.Reviewer.FullName : "",
                TeacherId = r.TeacherId,
                TeacherName = r.Teacher != null
                    ? r.Teacher.FullName : "",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAtUtc = r.CreatedAtUtc,
                UpdatedAtUtc = r.UpdatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<List<ReviewResponse>> GetMyReviewsAsync(
        string reviewerId)
    {
        return await _context.Reviews
            .AsNoTracking()
            .Where(r => r.ReviewerId == reviewerId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new ReviewResponse
            {
                Id = r.Id,
                BookingId = r.BookingId,
                ReviewerId = r.ReviewerId,
                ReviewerName = r.Reviewer != null
                    ? r.Reviewer.FullName : "",
                TeacherId = r.TeacherId,
                TeacherName = r.Teacher != null
                    ? r.Teacher.FullName : "",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAtUtc = r.CreatedAtUtc,
                UpdatedAtUtc = r.UpdatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<ReviewResponse?> UpdateAsync(
        int id, UpdateReviewRequest request, string reviewerId)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r =>
                r.Id == id && r.ReviewerId == reviewerId);

        if (review == null)
            return null;

        review.Rating = request.Rating;
        review.Comment = request.Comment?.Trim();
        review.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await BuildResponseAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, string reviewerId)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r =>
                r.Id == id && r.ReviewerId == reviewerId);

        if (review == null)
            return false;

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<double?> GetAverageRatingAsync(string teacherId)
    {
        var ratings = _context.Reviews
            .Where(r => r.TeacherId == teacherId)
            .Select(r => (double)r.Rating);

        if (!await ratings.AnyAsync())
            return null;

        return await ratings.AverageAsync();
    }

    private async Task<ReviewResponse> BuildResponseAsync(int id)
    {
        return await _context.Reviews
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ReviewResponse
            {
                Id = r.Id,
                BookingId = r.BookingId,
                ReviewerId = r.ReviewerId,
                ReviewerName = r.Reviewer != null
                    ? r.Reviewer.FullName : "",
                TeacherId = r.TeacherId,
                TeacherName = r.Teacher != null
                    ? r.Teacher.FullName : "",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAtUtc = r.CreatedAtUtc,
                UpdatedAtUtc = r.UpdatedAtUtc
            })
            .FirstAsync();
    }
}
