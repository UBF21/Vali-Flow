using System.Linq.Expressions;
using ValiFlow.Core.Classes.Base;
using ValiFlow.Core.Interfaces.Types;

namespace ValiFlow.Core.Classes.Types;

public class DateTimeExpression<TBuilder, T> : IDateTimeExpression<TBuilder, T>
    where TBuilder : BaseExpression<TBuilder, T>, IDateTimeExpression<TBuilder, T>, new()
{
     private readonly BaseExpression<TBuilder, T> _builder;

    public DateTimeExpression(BaseExpression<TBuilder, T> builder)
    {
        _builder = builder;
    }

    public TBuilder FutureDate(Expression<Func<T, DateTime>> selector)
    {
        Expression<Func<DateTime, bool>> predicate = val => val > DateTime.Now;
        return _builder.Add(selector, predicate);
    }

    public TBuilder PastDate(Expression<Func<T, DateTime>> selector)
    {
        Expression<Func<DateTime, bool>> predicate = val => val < DateTime.Now;
        return _builder.Add(selector, predicate);
    }

    public TBuilder BetweenDates(Expression<Func<T, DateTime>> selector, DateTime startDate, DateTime endDate)
    {
        Expression<Func<DateTime, bool>> predicate = val => val.Date >= startDate && val.Date <= endDate;
        return _builder.Add(selector, predicate);
    }

    public TBuilder BetweenDates(Expression<Func<T, DateTime>> selector,
        Expression<Func<T, DateTime>> startDateSelector, Expression<Func<T, DateTime>> endDateSelector)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (startDateSelector == null) throw new ArgumentNullException(nameof(startDateSelector));
        if (endDateSelector == null) throw new ArgumentNullException(nameof(endDateSelector));

        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");

        InvocationExpression selectorBody = Expression.Invoke(selector, parameter);
        InvocationExpression startDateBody = Expression.Invoke(startDateSelector, parameter);
        InvocationExpression endDateBody = Expression.Invoke(endDateSelector, parameter);

        Expression<Func<T, bool>> filter = Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(
                Expression.GreaterThanOrEqual(selectorBody, startDateBody),
                Expression.LessThanOrEqual(selectorBody, endDateBody)
            ),
            parameter
        );

        return _builder.Add(filter);
    }

    public TBuilder ExactDate(Expression<Func<T, DateTime>> selector, DateTime date)
    {
        Expression<Func<DateTime, bool>> predicate = val => val.Date == date.Date;
        return _builder.Add(selector, predicate);
    }

    public TBuilder BeforeDate(Expression<Func<T, DateTime>> selector, DateTime date)
    {
        Expression<Func<DateTime, bool>> predicate = val => val < date;
        return _builder.Add(selector, predicate);
    }

    public TBuilder AfterDate(Expression<Func<T, DateTime>> selector, DateTime date)
    {
        Expression<Func<DateTime, bool>> predicate = val => val > date;
        return _builder.Add(selector, predicate);
    }

    public TBuilder IsToday(Expression<Func<T, DateTime>> selector)
    {
        Expression<Func<DateTime, bool>> predicate = val => val.Date == DateTime.Today;
        return _builder.Add(selector, predicate);
    }

    public TBuilder IsYesterday(Expression<Func<T, DateTime>> selector)
    {
        Expression<Func<DateTime, bool>> predicate = val => val.Date == DateTime.Today.AddDays(-1);
        return _builder.Add(selector, predicate);
    }

    public TBuilder IsTomorrow(Expression<Func<T, DateTime>> selector)
    {
        Expression<Func<DateTime, bool>> predicate = val => val.Date == DateTime.Today.AddDays(1);
        return _builder.Add(selector, predicate);
    }

    public TBuilder InLastDays(Expression<Func<T, DateTime>> selector, int days)
    {
        if (days <= 0)
            throw new ArgumentOutOfRangeException(
                $"Not Permited days in zero and days necesary in positive {nameof(days)}");

        Expression<Func<DateTime, bool>> predicate = val => val.Date >= DateTime.Today.AddDays(-days);
        return _builder.Add(selector, predicate);
    }

    public TBuilder InNextDays(Expression<Func<T, DateTime>> selector, int days)
    {
        if (days <= 0)
            throw new ArgumentOutOfRangeException(
                $"Not Permited days in zero and days necesary in positive {nameof(days)}");

        Expression<Func<DateTime, bool>> predicate = val => val.Date <= DateTime.Today.AddDays(days);
        return _builder.Add(selector, predicate);
    }

    public TBuilder IsWeekend(Expression<Func<T, DateTime>> selector)
    {
        Expression<Func<DateTime, bool>> predicate = val =>
            val.DayOfWeek == DayOfWeek.Saturday || val.DayOfWeek == DayOfWeek.Sunday;
        return _builder.Add(selector, predicate);
    }

    public TBuilder IsWeekday(Expression<Func<T, DateTime>> selector)
    {
        Expression<Func<DateTime, bool>> predicate = val =>
            val.DayOfWeek >= DayOfWeek.Monday && val.DayOfWeek <= DayOfWeek.Friday;
        return _builder.Add(selector, predicate);
    }

    public TBuilder IsLeapYear(Expression<Func<T, DateTime>> selector)
    {
        Expression<Func<DateTime, bool>> predicate = val => DateTime.IsLeapYear(val.Year);
        return _builder.Add(selector, predicate);
    }

    public TBuilder SameMonthAs(Expression<Func<T, DateTime>> selector, DateTime date)
    {
        Expression<Func<DateTime, bool>> predicate = val => val.Year == date.Year && val.Month == date.Month;
        return _builder.Add(selector, predicate);
    }

    public TBuilder SameYearAs(Expression<Func<T, DateTime>> selector, DateTime date)
    {
        Expression<Func<DateTime, bool>> predicate = val => val.Year == date.Year;
        return _builder.Add(selector, predicate);
    }
}