﻿using System;
using SqlSelectBuilder;
using SqlSelectBuilder.SqlFilter;
using Tests.Entities;
using Xunit;

namespace Tests
{
    public class SqlSelectTest
    {
        [Fact]
        public void IncorrectAliasThrowsException()
        {
            var select = new SqlSelect<Person>()
                .InnerJoin<Person, Passport>((person, passport) => person.Id == passport.PersonId)
                .AddField(SqlField<Person>.Count(p => p.Id, "pa"));
            Assert.Throws<InvalidOperationException>(() => { var cmd = select.CommandText; });
        }

        [Fact]
        public void SimpleSelect()
        {
            var select = new SqlSelect<Person>();

            var expected =
@"SELECT
    *
FROM
    Person pe";
            Assert.Equal(expected, select.CommandText);
        }

        [Fact]
        public void DistinctAndTop()
        {
            var select = new SqlSelect<Person>()
                .AddFields(p => p.Id)
                .Distinct()
                .Top(5);

            var expected =
@"SELECT
    DISTINCT TOP 5 pe.Id
FROM
    Person pe";
            Assert.Equal(expected, select.CommandText);
        }

        [Fact]
        public void TopPercent()
        {
            var select = new SqlSelect<Person>()
                .AddFields(p => p.Id)
                .Top(5, true);

            var expected =
@"SELECT
    TOP 5 PERCENT pe.Id
FROM
    Person pe";
            Assert.Equal(expected, select.CommandText);
        }

        [Fact]
        public void Where()
        {
            var select = new SqlSelect<Person>()
                .AddFields(p => p.Id)
                .Where(SqlFilter<Person>.From(p => p.LastName).IsNotNull());

            var expected =
@"SELECT
    pe.Id
FROM
    Person pe
WHERE
    pe.LastName IS NOT NULL";
            Assert.Equal(expected, select.CommandText);
        }

        [Fact]
        public void GroupBy()
        {
            var select = new SqlSelect<Person>()
                .AddField(SqlField<Person>.Count(p => p.Id))
                .GroupBy(p => p.LastName);

            var expected =
@"SELECT
    COUNT(pe.Id)
FROM
    Person pe
GROUP BY
    pe.LastName";
            Assert.Equal(expected, select.CommandText);
        }

        [Fact]
        public void Having()
        {
            var select = new SqlSelect<Person>()
                .AddField(SqlField<Person>.Count(p => p.Id))
                .GroupBy(p => p.LastName)
                .Having(SqlFilter<Person>.From<int>(SqlField<Person>.Count(p => p.Id)).GreaterThan(2));

            var expected =
@"SELECT
    COUNT(pe.Id)
FROM
    Person pe
GROUP BY
    pe.LastName
HAVING
    COUNT(pe.Id) > 2";
            Assert.Equal(expected, select.CommandText);
        }

        [Fact]
        public void OrderBy()
        {
            var select = new SqlSelect<Person>()
                .Where(SqlFilter<Person>.From(p => p.LastName).IsNotNull())
                .OrderBy(p => p.Id);

            var expected =
@"SELECT
    *
FROM
    Person pe
WHERE
    pe.LastName IS NOT NULL
ORDER BY
    pe.Id";
            Assert.Equal(expected, select.CommandText);
        }

        [Fact]
        public void Joins()
        {
            var joinByLambdaQry = new SqlSelect<Person>()
                .InnerJoin<Person, Passport>((person, passport) => person.Id == passport.PersonId);
            var joinByFilterQry = new SqlSelect<Person>()
                .InnerJoin<Passport>(SqlFilter<Person>.From(p => p.Id).EqualTo<Passport>(p => p.PersonId));

            var expected =
@"SELECT
    *
FROM
    Person pe
INNER JOIN
    Passport pa ON pe.Id = pa.PersonId";
            Assert.Equal(expected, joinByLambdaQry.CommandText);
            Assert.Equal(expected, joinByFilterQry.CommandText);
        }
    }
}
