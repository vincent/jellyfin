﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class GenresHandler : BaseSerializationHandler<IBNItem[]>
    {
        protected override Task<IBNItem[]> GetObjectToSerialize()
        {
            Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            Guid userId = Guid.Parse(QueryString["userid"]);
            User user = Kernel.Instance.Users.First(u => u.Id == userId);

            return GetAllGenres(parent, user);
        }

        /// <summary>
        /// Gets all genres from all recursive children of a folder
        /// The CategoryInfo class is used to keep track of the number of times each genres appears
        /// </summary>
        private async Task<IBNItem[]> GetAllGenres(Folder parent, User user)
        {
            Dictionary<string, int> data = new Dictionary<string, int>();

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetParentalAllowedRecursiveChildren(user);

            foreach (var item in allItems)
            {
                // Add each genre from the item to the data dictionary
                // If the genre already exists, increment the count
                if (item.Genres == null)
                {
                    continue;
                }

                foreach (string val in item.Genres)
                {
                    if (!data.ContainsKey(val))
                    {
                        data.Add(val, 1);
                    }
                    else
                    {
                        data[val]++;
                    }
                }
            }

            // Get the Genre objects
            Genre[] entities = await Task.WhenAll<Genre>(data.Keys.Select(key => { return Kernel.Instance.ItemController.GetGenre(key); })).ConfigureAwait(false);

            // Convert to an array of IBNItem
            IBNItem[] items = new IBNItem[entities.Length];

            for (int i = 0; i < entities.Length; i++)
            {
                Genre e = entities[i];

                items[i] = ApiService.GetIBNItem(e, data[e.Name]);
            }

            return items;
        }
    }
}
