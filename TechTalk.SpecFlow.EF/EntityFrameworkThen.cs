using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow.Assist;
using TechTalk.SpecFlow;
using System.Reflection;

namespace TechTalk.SpecFlow.EF
{
    public class EntityFrameworkThen<T> where T:class
    {
        private DbContext dbContext;
        private Expression<Func<T, object>>[] lookupProperties;
        public EntityFrameworkThen(DbContext dbContext, params Expression<Func<T,object>>[] lookupProperties)
        {
            this.dbContext = dbContext;
            this.lookupProperties = lookupProperties;
        }

        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(TSource source, Expression<Func<TSource, TProperty>> propertyLambda)
        {
            Type type = typeof(TSource);

            MemberExpression member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    propertyLambda.ToString()));

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    propertyLambda.ToString()));

            if (type != propInfo.ReflectedType &&
                !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException(string.Format(
                    "Expresion '{0}' refers to a property that is not from type {1}.",
                    propertyLambda.ToString(),
                    type));

            return propInfo;
        }

        public List<T> Execute(Table table)
        {
            List<T> entities = table.CreateSet<T>().ToList();
            Expression<Func<T, object>>[] lookupProperties = null;
            Expression<Func<T, object>>[] propertiesToCheck = null;
            List<T> foundEntities = new List<T>();
            foreach (T entity in entities)
            {
                var lookupExpression = BuildLookupExpression(entity, lookupProperties);
                T foundEntity = dbContext.Set<T>().FirstOrDefault(lookupExpression);
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
            ParameterExpression pe = Expression.Parameter(typeof(T), "x");
            Expression left = null;

            foreach (var lookupPropertyValue in lookupPropertiesValues)
            {
                Expression property = Expression.Property(pe, lookupPropertyValue.Key);
                Expression constant = Expression.Constant(lookupPropertyValue.Value);
                Expression equality = Expression.Equal(property, Expression.Convert(constant, property.Type));

                if (left == null)
                {
                    left = equality;
                }
                else
                {
                    left = Expression.AndAlso(left, equality);
                }
            }
            return Expression.Lambda<Func<T, bool>>(left, new ParameterExpression[] { pe });

        }

        private static Expression<Func<T,bool>> BuildLookupExpression(T entity, Expression<Func<T, object>>[] lookupProperties)
        {
            Dictionary<string, object> lookupPropertiesValues = new Dictionary<string, object>();
            foreach(var lookupProperty in lookupProperties)
            {
                PropertyInfo propertyInfo = GetPropertyInfo(entity, lookupProperty);
                if (propertyInfo == null)
                    throw new Exception("Invalid lookup property");
                lookupPropertiesValues.Add(propertyInfo.Name, propertyInfo.GetValue(entity));
            }
            return BuildLookupExpression(lookupPropertiesValues);
        }

        private void CheckEntity(T entity, T entityToCheck, Expression<Func<T, object>>[] propertiesToCheck)
        {
            foreach(var propertyToCheck in propertiesToCheck)
            {
                var func = propertyToCheck.Compile();
                if (func(entity) == func(entityToCheck))
                    continue;
                throw new Exception("Invalid property value");
            }
        }
    }
}
