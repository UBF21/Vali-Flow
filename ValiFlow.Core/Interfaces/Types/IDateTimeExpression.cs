using System.Linq.Expressions;

namespace ValiFlow.Core.Interfaces.Types;

/// <summary>
/// Defines a set of date-related validation expressions.
/// </summary>
/// <typeparam name="TBuilder">The type of the builder that allows method chaining.</typeparam>
/// <typeparam name="T">The type of the entity being validated.</typeparam>
public interface IDateTimeExpression<out TBuilder, T>
{
     /// <summary>
    /// Ensures that the selected date is in the future.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    TBuilder FutureDate(Expression<Func<T, DateTime>> selector);
    
    /// <summary>
    /// Ensures that the selected date is in the past.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    TBuilder PastDate(Expression<Func<T, DateTime>> selector);
    
    /// <summary>
    /// Ensures that the selected date is within a specific range.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    /// <param name="startDate">The start of the date range.</param>
    /// <param name="endDate">The end of the date range.</param>
    TBuilder BetweenDates(Expression<Func<T, DateTime>> selector, DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Ensures that the selected date falls between two other dates selected dynamically from the entity.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    /// <param name="startDateSelector">Expression to select the start date dynamically.</param>
    /// <param name="endDateSelector">Expression to select the end date dynamically.</param>
    TBuilder BetweenDates(Expression<Func<T, DateTime>> selector, Expression<Func<T, DateTime>> startDateSelector,
        Expression<Func<T, DateTime>> endDateSelector);
    
    /// <summary>
    /// Ensures that the selected date matches a specific date.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    /// <param name="date">The exact date required.</param>
    TBuilder ExactDate(Expression<Func<T, DateTime>> selector, DateTime date);
    
    /// <summary>
    /// Ensures that the selected date is before a specified date.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    /// <param name="date">The date to compare against.</param>
    TBuilder BeforeDate(Expression<Func<T, DateTime>> selector, DateTime date);
    
    /// <summary>
    /// Ensures that the selected date is after a specified date.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    /// <param name="date">The date to compare against.</param>
    TBuilder AfterDate(Expression<Func<T, DateTime>> selector, DateTime date);
    
    /// <summary>
    /// Ensures that the selected date is today's date.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    TBuilder IsToday(Expression<Func<T, DateTime>> selector);
    
    /// <summary>
    /// Ensures that the selected date corresponds to yesterday's date.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    TBuilder IsYesterday(Expression<Func<T, DateTime>> selector);
    
    /// <summary>
    /// Ensures that the selected date corresponds to tomorrow's date.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    TBuilder IsTomorrow(Expression<Func<T, DateTime>> selector);
    
    /// <summary>
    /// Ensures that the selected date falls within the last specified number of days from today.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    /// <param name="days">The number of past days to check within.</param>
    TBuilder InLastDays(Expression<Func<T, DateTime>> selector, int days);
    
    /// <summary>
    /// Ensures that the selected date falls within the next specified number of days from today.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    /// <param name="days">The number of future days to check within.</param>
    TBuilder InNextDays(Expression<Func<T, DateTime>> selector, int days);
    
    /// <summary>
    /// Ensures that the selected date falls on a weekend (Saturday or Sunday).
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    TBuilder IsWeekend(Expression<Func<T, DateTime>> selector);
    
    /// <summary>
    /// Ensures that the selected date falls on a weekday (Monday to Friday).
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    TBuilder IsWeekday(Expression<Func<T, DateTime>> selector);
    
    /// <summary>
    /// Ensures that the selected date falls in a leap year.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    TBuilder IsLeapYear(Expression<Func<T, DateTime>> selector);
    
    /// <summary>
    /// Ensures that the selected date falls in the same month as the specified date.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    /// <param name="date">The date to compare the month with.</param>
    TBuilder SameMonthAs(Expression<Func<T, DateTime>> selector, DateTime date);
    
    /// <summary>
    /// Ensures that the selected date falls in the same year as the specified date.
    /// </summary>
    /// <param name="selector">Expression to select the date property.</param>
    /// <param name="date">The date to compare the year with.</param>
    TBuilder SameYearAs(Expression<Func<T, DateTime>> selector, DateTime date);
}