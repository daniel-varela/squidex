﻿// ==========================================================================
//  MongoContentRepository.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OData.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using Squidex.Infrastructure;
using Squidex.Infrastructure.CQRS.Events;
using Squidex.Read.Apps;
using Squidex.Read.Contents;
using Squidex.Read.Contents.Builders;
using Squidex.Read.Contents.Repositories;
using Squidex.Read.MongoDb.Contents.Visitors;
using Squidex.Read.Schemas;
using Squidex.Read.Schemas.Services;

namespace Squidex.Read.MongoDb.Contents
{
    public partial class MongoContentRepository : IContentRepository, IEventConsumer
    {
        private const string Prefix = "Projections_Content_";
        private readonly IMongoDatabase database;
        private readonly ISchemaProvider schemas;
        private readonly EdmModelBuilder modelBuilder;

        protected static FilterDefinitionBuilder<MongoContentEntity> Filter
        {
            get
            {
                return Builders<MongoContentEntity>.Filter;
            }
        }

        protected static UpdateDefinitionBuilder<MongoContentEntity> Update
        {
            get
            {
                return Builders<MongoContentEntity>.Update;
            }
        }

        protected static ProjectionDefinitionBuilder<MongoContentEntity> Projection
        {
            get
            {
                return Builders<MongoContentEntity>.Projection;
            }
        }

        protected static IndexKeysDefinitionBuilder<MongoContentEntity> Index
        {
            get
            {
                return Builders<MongoContentEntity>.IndexKeys;
            }
        }

        public MongoContentRepository(IMongoDatabase database, ISchemaProvider schemas, EdmModelBuilder modelBuilder)
        {
            Guard.NotNull(database, nameof(database));
            Guard.NotNull(modelBuilder, nameof(modelBuilder));
            Guard.NotNull(schemas, nameof(schemas));

            this.schemas = schemas;
            this.database = database;
            this.modelBuilder = modelBuilder;
        }

        public async Task<IReadOnlyList<IContentEntity>> QueryAsync(Guid schemaId, bool nonPublished, HashSet<Guid> ids, string odataQuery, IAppEntity appEntity)
        {
            var contentEntities = (List<IContentEntity>)null;

            await ForSchemaAsync(appEntity.Id, schemaId, async (collection, schemaEntity) =>
            {
                IFindFluent<MongoContentEntity, MongoContentEntity> cursor;
                try
                {
                    var model = modelBuilder.BuildEdmModel(schemaEntity, appEntity);

                    var parser = model.ParseQuery(odataQuery);

                    cursor = 
                        collection
                            .Find(parser, ids, schemaEntity.Id, schemaEntity.Schema, nonPublished)
                            .Take(parser)
                            .Skip(parser)
                            .Sort(parser, schemaEntity.Schema);
                }
                catch (NotSupportedException)
                {
                    throw new ValidationException("This odata operation is not supported");
                }
                catch (NotImplementedException)
                {
                    throw new ValidationException("This odata operation is not supported");
                }
                catch (ODataException ex)
                {
                    throw new ValidationException("Failed to parse query: " + ex.Message, ex);
                }

                var entities = await cursor.ToListAsync();

                foreach (var entity in entities)
                {
                    entity.ParseData(schemaEntity.Schema);
                }

                contentEntities = entities.OfType<IContentEntity>().ToList();
            });

            return contentEntities;
        }

        public async Task<long> CountAsync(Guid schemaId, bool nonPublished, HashSet<Guid> ids, string odataQuery, IAppEntity appEntity)
        {
            var contentsCount = 0L;

            await ForSchemaAsync(appEntity.Id, schemaId, async (collection, schemaEntity) =>
            {
                IFindFluent<MongoContentEntity, MongoContentEntity> cursor;
                try
                {
                    var model = modelBuilder.BuildEdmModel(schemaEntity, appEntity);

                    var parser = model.ParseQuery(odataQuery);

                    cursor = collection.Find(parser, ids, schemaEntity.Id, schemaEntity.Schema, nonPublished);
                }
                catch (NotSupportedException)
                {
                    throw new ValidationException("This odata operation is not supported");
                }
                catch (NotImplementedException)
                {
                    throw new ValidationException("This odata operation is not supported");
                }
                catch (ODataException ex)
                {
                    throw new ValidationException("Failed to parse query: " + ex.Message, ex);
                }

                contentsCount = await cursor.CountAsync();
            });

            return contentsCount;
        }

        public async Task<IReadOnlyList<Guid>> QueryNotFoundAsync(Guid appId, Guid schemaId, IList<Guid> contentIds)
        {
            var contentEntities = (List<BsonDocument>)null;

            await ForAppIdAsync(appId, async collection =>
            {
                contentEntities =
                    await collection.Find(x => contentIds.Contains(x.Id) && x.AppId == appId).Project<BsonDocument>(Projection.Include(x => x.Id))
                        .ToListAsync();
            });

            return contentIds.Except(contentEntities.Select(x => x["Id"].AsGuid)).ToList();
        }

        public async Task<IContentEntity> FindContentAsync(Guid schemaId, Guid id, IAppEntity appEntity)
        {
            var contentEntity = (MongoContentEntity)null;

            await ForSchemaAsync(appEntity.Id, schemaId, async (collection, schemaEntity) =>
            {
                contentEntity = 
                    await collection.Find(x => x.Id == id)
                        .FirstOrDefaultAsync();

                contentEntity?.ParseData(schemaEntity.Schema);
            });

            return contentEntity;
        }

        private async Task ForSchemaAsync(Guid appId, Guid schemaId, Func<IMongoCollection<MongoContentEntity>, ISchemaEntity, Task> action)
        {
            var collection = GetCollection(appId);

            var schemaEntity = await schemas.FindSchemaByIdAsync(schemaId, true);

            if (schemaEntity == null)
            {
                return;
            }

            await action(collection, schemaEntity);
        }
    }
}
