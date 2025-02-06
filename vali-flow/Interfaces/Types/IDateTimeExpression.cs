using System.Linq.Expressions;

namespace vali_flow.Interfaces.Types;

public interface IDateTimeExpression<out TBuilder, T>
{
    TBuilder FutureDate(Expression<Func<T, DateTime>> selector);
    TBuilder PastDate(Expression<Func<T, DateTime>> selector);
    TBuilder BetweenDates(Expression<Func<T, DateTime>> selector, DateTime startDate, DateTime endDate);

    TBuilder BetweenDates(Expression<Func<T, DateTime>> selector, Expression<Func<T, DateTime>> startDateSelector,
        Expression<Func<T, DateTime>> endDateSelector);

    TBuilder ExactDate(Expression<Func<T, DateTime>> selector, DateTime date);
    TBuilder BeforeDate(Expression<Func<T, DateTime>> selector, DateTime date);
    TBuilder AfterDate(Expression<Func<T, DateTime>> selector, DateTime date);
    TBuilder IsToday(Expression<Func<T, DateTime>> selector);
    TBuilder IsYesterday(Expression<Func<T, DateTime>> selector);
    TBuilder IsTomorrow(Expression<Func<T, DateTime>> selector);
    TBuilder InLastDays(Expression<Func<T, DateTime>> selector, int days);
    TBuilder InNextDays(Expression<Func<T, DateTime>> selector, int days);
    TBuilder IsWeekend(Expression<Func<T, DateTime>> selector);
    TBuilder IsWeekday(Expression<Func<T, DateTime>> selector);
    TBuilder IsLeapYear(Expression<Func<T, DateTime>> selector);
    TBuilder SameMonthAs(Expression<Func<T, DateTime>> selector, DateTime date);
    TBuilder SameYearAs(Expression<Func<T, DateTime>> selector, DateTime date);
}