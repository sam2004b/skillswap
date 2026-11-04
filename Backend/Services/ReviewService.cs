using Backend.Data;
using Backend.DTOs.Reviews;
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

    public async Task<ReviewResponse> CreateReviewAsync(
        string reviewerId,
        CreateReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
        {
            throw new InvalidOperationException(
                "Rating must be between 1 and 5.");
        }

        var reviewerExists = await _context.Users
            .AnyAsync(u => u.Id == reviewerId && u.IsActive);

        if (!reviewerExists)
        {
            throw new UnauthorizedAccessException(
                "Your account is not active.");
        }

        var booking = await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BookingId);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found.");
        }

        if (booking.StudentId != reviewerId)
        {
            throw new UnauthorizedAccessException(
                "Only the student who made the booking can review it.");
        }

        if (booking.Status != "Completed")
        {
            throw new InvalidOperationException(
                "You can only review completed bookings.");
        }

        if (booking.TeacherId == reviewerId)
        {
            throw new InvalidOperationException(
                "You cannot review yourself.");
        }

        var alreadyReviewed = await _context.Reviews
            .AnyAsync(r => r.BookingId == request.BookingId);

        if (alreadyReviewed)
        {
            throw new InvalidOperationException(
                "This booking has already been reviewed.");
        }

        var comment = request.Comment?.Trim();

        if (comment != null && comment.Length > 2000)
        {
            throw new InvalidOperationException(
                "Review comment cannot exceed 2000 characters.");
        }

        var review = new Review
        {
            BookingId = booking.Id,
            ReviewerId = reviewerId,
            TeacherId = booking.TeacherId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(comment)
                ? null
                : comment,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Reviews.Add(review);

        await _context.SaveChangesAsync();

        return await GetReviewResponseAsync(review.Id);
    }

    public async Task<TeacherReviewsResponse> GetTeacherReviewsAsync(
        string teacherId)
    {
        var teacher = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == teacherId && u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.FullName
            })
            .FirstOrDefaultAsync();

        if (teacher == null)
        {
            throw new KeyNotFoundException("Teacher not found.");
        }

        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.TeacherId == teacherId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new ReviewResponse
            {
                Id = r.Id,
                BookingId = r.BookingId,
                ReviewerId = r.ReviewerId,
                ReviewerName = r.Reviewer!.FullName,
                TeacherId = r.TeacherId,
                TeacherName = r.Teacher!.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAtUtc = r.CreatedAtUtc
            })
            .ToListAsync();

        return new TeacherReviewsResponse
        {
            TeacherId = teacher.Id,
            TeacherName = teacher.FullName,
            AverageRating = reviews.Count == 0
                ? 0
                : Math.Round(reviews.Average(r => r.Rating), 2),
            TotalReviews = reviews.Count,
            Reviews = reviews
        };
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
                ReviewerName = r.Reviewer!.FullName,
                TeacherId = r.TeacherId,
                TeacherName = r.Teacher!.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAtUtc = r.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<ReviewResponse?> GetReviewByIdAsync(int id)
    {
        return await _context.Reviews
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ReviewResponse
            {
                Id = r.Id,
                BookingId = r.BookingId,
                ReviewerId = r.ReviewerId,
                ReviewerName = r.Reviewer!.FullName,
                TeacherId = r.TeacherId,
                TeacherName = r.Teacher!.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAtUtc = r.CreatedAtUtc
            })
            .FirstOrDefaultAsync();
    }

    public async Task DeleteReviewAsync(int id, string reviewerId)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
        {
            throw new KeyNotFoundException("Review not found.");
        }

        if (review.ReviewerId != reviewerId)
        {
            throw new UnauthorizedAccessException(
                "You can only delete reviews you wrote.");
        }

        _context.Reviews.Remove(review);

        await _context.SaveChangesAsync();
    }

    private async Task<ReviewResponse> GetReviewResponseAsync(int id)
    {
        var response = await GetReviewByIdAsync(id);

        if (response == null)
        {
            throw new KeyNotFoundException("Review not found.");
        }

        return response;
    }
}