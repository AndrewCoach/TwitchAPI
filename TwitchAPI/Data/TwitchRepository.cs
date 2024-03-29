﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchAPI.Models;

namespace TwitchAPI.Data
{
    public class TwitchRepository : ITwitchRepository
    {
        private readonly TwitchContext _context;

        public TwitchRepository(TwitchContext context)
        {
            _context = context;
        }

        public void CreateApp(App app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            _context.Apps.Add(app);
        }

        public void CreateUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            _context.TwitchUsers.Add(user);
        }

        public void DeleteApp(App app)
        {
            _context.Apps.Remove(app);
        }

        public void DeleteUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            _context.TwitchUsers.Remove(user);
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _context.TwitchUsers.ToList();
        }

        public App GetApp()
        {
            return _context.Apps.FirstOrDefault();
        }

        public User GetUserByUserId(int id)
        {
            return _context.TwitchUsers.FirstOrDefault(user => user.UserId == id);
        }

        public User GetUserByDBId(int id)
        {
            return _context.TwitchUsers.FirstOrDefault(user => user.Id == id);
        }

        public bool SaveChanges()
        {
            return _context.SaveChanges() > 0;
        }
    }
}
