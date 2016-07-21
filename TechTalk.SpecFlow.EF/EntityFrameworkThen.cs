using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using TechTalk.SpecFlow.Assist;
using System.Reflection;

namespace TechTalk.SpecFlow.EF
{
    public class EntityFrameworkThen<T> where T:class
    {
        private readonly DbContext _dbContext;
        private Expression<Func<T, object>>[] _lookupProperties;

        public EntityFrameworkThen(DbContext dbContext, params Expression<Func<T,object>>[] lookupProperties)
        {
            this._dbContext = dbContext;
            this._lookupProperties = lookupProperties;
        }

        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(TSource source, Expression<Func<TSource, TProperty>> propertyLambda)
        {
            var type = typeof(TSource);

            var member = propertyLambda.Body as MemberExpression;
            if (member == null)
            {
                throw new ArgumentException(string.Format("Expression '{0}' refers to a method, not a property.", propertyLambda));
            }

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
            {
                throw new ArgumentException(string.Format("Expression '{0}' refers to a field, not a property.", propertyLambda));
            }


            if (type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType))
            {
                throw new ArgumentException(string.Format("Expresion '{0}' refers to a property that is not from type {1}.", propertyLambda, type));
            }
                
            return propInfo;
        }

        public List<T> Execute(Table table)
        {
            var entities = table.CreateSet<T>().ToList();
            Expression<Func<T, object>>[] lookupProperties = null;
            Expression<Func<T, object>>[] propertiesToCheck = null;
            var foundEntities = new List<T>();

            foreach (var entity in entities)
            {
                var lookupExpression = BuildLookupExpression(entity, lookupProperties);
                var foundEntity = _dbContext.Set<T>().FirstOrDefault(lookupExpression);

                if (foundEntity == null)
                {
                    throw new Exception("Entity not found at row :" + entities.IndexOf(entity));
                }

                CheckEntity(entity, foundEntity, propertiesToCheck);

                foundEntities.Add(foundEntity);
            }

            return foundEntities;
        }

        private static Expression<Func<T,bool>> BuildLookupExpression(Dictionary<string,object> lookupPropertiesValues)
        {
            var pe = Expression.Parameter(typeof(T), "x");
            var left = (from lookupPropertyValue in lookupPropertiesValues
                        let property = Expression.Property(pe, lookupPropertyValue.Key)
                        let constant = Expression.Constant(lookupPropertyValue.Value)
                        select Expression.Equal(property, Expression.Convert(constant, property.Type)))
                            .Aggregate<Expression, Expression>(null, (current, equality) => current == null 
                                ? equality 
                                : Expression.AndAlso(current, equality));

            return Expression.Lambda<Func<T, bool>>(left, pe);
        }

        private static Expression<Func<T,bool>> BuildLookupExpression(T entity, Expression<Func<T, object>>[] lookupProperties)
        {
            var lookupPropertiesValues = new Dictionary<string, object>();

            foreach(var lookupProperty in lookupProperties)
            {
                var propertyInfo = GetPropertyInfo(entity, lookupProperty);

                if (propertyInfo == null)
                {
                    throw new Exception("Invalid lookup property");
                }

                lookupPropertiesValues.Add(propertyInfo.Name, propertyInfo.GetValue(entity));
            }

            return BuildLookupExpression(lookupPropertiesValues);
        }

        private void CheckEntity(T entity, T entityToCheck, Expression<Func<T, object>>[] propertiesToCheck)
        {
            if (propertiesToCheck
                .Select(propertyToCheck => propertyToCheck.Compile())
                .Any(func => func(entity) != func(entityToCheck)))
            {
                throw new Exception("Invalid property value");
            }
        }
    }
}
