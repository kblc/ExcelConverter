﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExelConverter.Core.DataObjects;
using ExelConverter.Core.Converter;
using Helpers;
using ExcelConverter.Parser;
using ExelConverter.Core.ExelDataReader;
using System.Data.Entity.Validation;

namespace ExelConverter.Core.DataAccess
{
    public class DataAccess : IDataAccess
    {
        public DataAccess() { }

        private static bool? isNoLocking = null;
        private static bool IsNoLocking
        {
            get
            {
                if (isNoLocking == null)
                {
                    isNoLocking = Environment.GetCommandLineArgs().Any(i => i.Like("*noLocks"));
                }
                return isNoLocking.Value;
            }
        }

        #region Main Database

        public string[] GetSidesNames()
        {
            //var dc = alphaEntities.Default;
            using (var dc = alphaEntities.New())
            {
                return dc.resource_sides
                    .Select(rs => rs.name)
                    .ToArray();
            }
        }

        public string[] GetLights()
        {
            //var dc = alphaEntities.Default;
            using (var dc = alphaEntities.New())
            {
                return dc.resource_lights.Select(l => l.name).ToArray();
            }
        }

        public Operator[] GetOperators(int[] ids = null)
        {
            Operator[] result = new Operator[] {};
            var logSession = Helpers.Old.Log.SessionStart("DataAccess.GetOperators()", true);
            bool wasException = false;
            try
            {
                int[] lIds = ids == null ? new int[] { } : ids;

                using (var dc = alphaEntities.New())
                {
                    result = dc.companies
                        .Where(c => c.type == "operator")
                        .Where(c => lIds.Count() == 0 || lIds.Contains((int)c.id))
                        .Select(c => 
                                new Operator()
                                    {
                                        Id = c.id,
                                        Name = c.name,
                                        Notes = c.notes
                                    }
                            )
                        .ToArray();
                }
            }
            catch(Exception ex)
            {
                wasException = true;
                Log.Add(ex.GetExceptionText());
            }
            finally
            {
                Helpers.Old.Log.SessionEnd(logSession, wasException);
            }
            return result;
        }

        public Operator GetOperator(int id)
        {
            return GetOperators(new int[] { id }).FirstOrDefault();
        }

        public DataObjects.Type[] GetTypes()
        {
            DataObjects.Type[] result = null;
            using (var dc = alphaEntities.New())
            {
                //var dc = alphaEntities.Default;
                result = dc.resource_types.Select(t => new DataObjects.Type
                {
                    Id = (int)t.id,
                    Name = t.name
                }).ToArray();
            }
            return result;
        }

        public Size[] GetSizes()
        {
            Size[] result = null;
            using (var dc = alphaEntities.New())
            {
                //var dc = alphaEntities.Default;
                result = dc.resource_sizes.Select(s => new Size
                {
                    Id = (int)s.id,
                    FkTypeId = (int)s.type_id,
                    Name = s.name
                }).ToArray();
            }
            return result;
        }

        public Size[] GetSizesByType(DataObjects.Type type)
        {
            Size[] result = null;
            using (var dc = alphaEntities.New())
            {
                //var dc = alphaEntities.Default;
                result = dc.resource_sizes.Where(s => s.type_id == type.Id).Select(s => new Size
                {
                    Id = (int)s.id,
                    FkTypeId = (int)s.type_id,
                    Name = s.name
                }).ToArray();
            }
            return result;
        }

        public City[] GetCities()
        {
            List<City> result = new List<City>();
            using (var dc = alphaEntities.New())
            {
                //var dc = alphaEntities.Default;
                result = new List<City>(
                    dc.cities.Select(c => new City
                    {
                        Id = (int)c.id,
                        Name = c.name
                    })
                );
            }
            return result.OrderBy(city => city.Name).ToArray();
        }

        public Region[] GetRegions()
        {
            List<Region> result = new List<Region>();
            using (var dc = alphaEntities.New())
            {
                //var dc = alphaEntities.Default;
                result = new List<Region>(
                    dc.regions.Select(r => new Region
                    {
                        Id = (int)r.id,
                        FkCityId = (int)r.city_id,
                        Name = r.name
                    })
                );
            }

            result = new List<Region>(result.OrderBy(region => region.Name));

            result.Add(new Region
            {
                Id = null,
                FkCityId = null,
                Name = Region.Unknown
            });

            return result.ToArray();
        }

        public Region[] GetRegionsByCity(City city)
        {
            Region[] result = null;
            using (var dc = alphaEntities.New())
            {
                //var dc = alphaEntities.Default;
                result = dc.regions.Where(r => r.city_id == city.Id).Select(r => new Region
                {
                    Id = (int)r.id,
                    FkCityId = (int)r.city_id,
                    Name = r.name
                }).ToArray();
            }
            return result;
        }

        public FillArea[] GetFillRects(int operatorId = -1, string type = null)
        {
            using (var dc = alphaEntities.New())
            {
                //var dc = alphaEntities.Default;
                var rects = 
                    dc
                    .import_rectangle
                    .Where(r => (operatorId == -1 || r.companyId == operatorId) && (type == null || r.type == type))
                    .Select(r => new FillArea
                        {
                            FKOperatorID = (int)r.companyId,
                            Height = r.height,
                            ID = (int)r.id,
                            Type = r.type,
                            Width = r.width,
                            X1 = r.x1,
                            X2 = r.x2,
                            Y1 = r.y1,
                            Y2 = r.y2
                        }).ToArray();
                return rects;
            }
        }

        public bool FillRectExists(FillArea area)
        {
            return FillRectExists(new FillArea[] { area }).FirstOrDefault();
        }

        public bool[] FillRectExists(FillArea[] areas)
        {
            var result = new bool[] { };
            using (var dc = alphaEntities.New())
            {
                var cmpIds = areas.Select(a => a.FKOperatorID).Distinct().ToArray();
                var rectangles = dc.import_rectangle.Where(r => cmpIds.Contains((int)r.companyId)).ToArray();
                result =
                    areas.Select(
                        area => rectangles.Any(rect => 
                            (int)rect.companyId == area.FKOperatorID &&
                            rect.height == area.Height && 
                            rect.width == area.Width && 
                            rect.type == area.Type &&
                            rect.x1 == area.X1 && 
                            rect.x2 == area.X2 && 
                            rect.y1 == area.Y1 && 
                            rect.y2 == area.Y2)
                        ).ToArray();
            }
            return result;
        }

        public void UpdateFillRectangle(FillArea area)
        {

        }

        public User[] GetUsers(string[] logins)
        {
            using (var dc = alphaEntities.New())
            {
                var users = logins == null
                    ? dc.users
                    : dc.users.Where(u => logins.Contains(u.login));

                //var dc = alphaEntities.Default;
                var result = users
                    .Select(c => new User() 
                        {
                            Id = c.id,
                            Login = c.login,
                            Password = c.password,
                            Name = c.name,
                            Surname = c.surname,
                            Patronymic = c.patronymic
                        }
                    ).ToArray();
                return result;
            }
        }

        public User GetUser(string login)
        {
            return GetUsers(new string[] { login }).FirstOrDefault();
        }


        #endregion
        #region App Database

        #region Parsers

        public Parser[] ParsersGet(Guid[] ids = null)
        {
            bool wasException = false;
            var result = new Parser[] { };
            var logSession = Helpers.Old.Log.SessionStart("DataAccess.ParsersGet()", true);
            try
            {
                var pIds = ids == null ? new Guid[] { } : ids;
                bool isEmptyIds = pIds.Length <= 0;

                using (var dc = exelconverterEntities2.New())
                {
                    var res = dc
                            .parsers
                            .Where(p => isEmptyIds || pIds.Contains(p.id))
                            .AsEnumerable()
                            .Select(parser =>
                            {
                                Parser p = Parser.DeserializeXML(parser.xml, typeof(Parser)) as Parser;
                                if (p != null)
                                    p.Id = parser.id;
                                return p;
                            })
                            .Where(p => p != null);

                    if (res.Count() > 0)
                        result = res.ToArray<Parser>();
                }
            }
            catch(Exception ex)
            {
                wasException = true;
                Helpers.Old.Log.Add(logSession, ex.GetExceptionText());
            }
            finally
            {
                Helpers.Old.Log.SessionEnd(logSession, wasException);
            }
            return result;
        }
        public Guid[] ParsersRemove(Parser[] parsers)
        {
            if (parsers != null)
                return ParsersRemove(parsers.Select(p => p.Id).ToArray());
            else
                return new Guid[] { };
        }
        public Guid[] ParsersRemove(Guid[] ids)
        {
            List<Guid> result = new List<Guid>();
            if (ids != null)
                using (var dc = exelconverterEntities2.New())
                {
                    //var dc = exelconverterEntities2.Default;
                    var parsersToRemove = dc.parsers.Where(p => ids.Contains(p.id)).ToArray();
                    if (parsersToRemove.Length > 0)
                    {
                        foreach (var p in parsersToRemove)
                        {
                            result.Add(p.id);
                            dc.parsers.Remove(p);
                        }
                        dc.SaveChanges();
                    }
                }
            return result.ToArray();
        }
        public Guid[] ParsersInsert(Parser[] parsers, bool needSave = true)
        {
            Guid[] result =new Guid[] { };
            if (parsers != null)
                using (var dc = exelconverterEntities2.New())
                {
                    var alreadyInserted = ParsersUpdate(parsers, false);
                    var parsersToInsert =
                            parsers
                            .Where(pr => !alreadyInserted.Contains(pr.Id))
                            .Select(p => new parsers
                                {
                                    id = p.Id,
                                    url = p.Url,
                                    xml = p.SerializeXML()
                                })
                            .ToArray();
                    result = parsersToInsert.Select(p => p.id).ToArray();

                    if (needSave)
                    { 
                        foreach (var p in parsersToInsert)
                            dc.parsers.Add(p);
                        dc.SaveChanges();
                    }
                }
            return result;
        }
        public Guid[] ParsersUpdate(Parser[] parsers, bool needSave = true)
        {
            Guid[] result = new Guid[] { };
            if (parsers != null)
                using (var dc = exelconverterEntities2.New())
                {
                    var ids = parsers.Select(p => p.Id).ToArray();
                    var parsersInDb = dc.parsers.Where(p => ids.Contains(p.id));

                    result = parsersInDb.Select(p => p.id).Distinct().ToArray();

                    if (needSave && parsersInDb.Count() > 0)
                    { 
                        foreach (var p in parsersInDb)
                        {
                            var pToUpdate = parsers.FirstOrDefault(p2 => p2.Id == p.id);
                            if (pToUpdate != null)
                            {
                                p.url = pToUpdate.Url;
                                p.xml = pToUpdate.SerializeXML();
                            }
                        }
                        dc.SaveChanges();
                    }
                }
            return result;
        }

        #endregion

        public SheetRulePair[] GetExportRulesIdByOperator(Operator op, IQueryable<ExelSheet> existedSheets)
        {
            var result = new SheetRulePair[] { };
            bool wasException = false;
            var logSesson = Helpers.Old.Log.SessionStart(string.Format("DataAccess.GetExportRulesIdByOperator(id:{0})", op == null ? "null" : op.Id.ToString()), true);
            try
            {
                using (var dc = exelconverterEntities2.New())
                {
                    string export_rule =
                        dc
                        .operator_export_rule
                        .Where(r => r.operator_id == op.Id && r.export_rules != null)
                        .Select(r=>r.export_rules)
                        .FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(export_rule))
                        result =
                            export_rule
                            .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(i => 
                                {
                                    var prts = i.Split(new char[] { ':' });
                                    if (prts.Length == 2)
                                        return new SheetRulePair(existedSheets)
                                            {
                                                Rule = op.MappingRules.FirstOrDefault(r => r.Id.ToString() == prts[0]),
                                                SheetName = prts[1].ToLower().Trim()
                                            };
                                        else return null;
                                })
                            .Where(i => i != null)
                            .ToArray();
                }
            }
            catch (Exception ex)
            {
                wasException = true;
                Helpers.Old.Log.Add(logSesson, ex.GetExceptionText());
                throw ex;
            }
            finally
            {
                Helpers.Old.Log.SessionEnd(logSesson, wasException);
            }

            return result;
        }

        public void SetExportedRulesForOperator(Operator op, SheetRulePair[] exportRules)
        {
            bool wasException = false;
            var logSesson = Helpers.Old.Log.SessionStart(string.Format("DataAccess.SetExportedRulesForOperator(id:{0})", op == null ? "null" : op.Id.ToString()), true);
            try
            {
                using (var dc = exelconverterEntities2.New())
                {
                    string ruleString = string.Empty;

                    var validRules = exportRules
                                        .Where(i => i.Rule != null && !string.IsNullOrWhiteSpace(i.Sheet?.Name ?? i.SheetName))
                                        .Select(i => string.Format("{0}:{1}", i.Rule.Id, i.Sheet?.Name ?? i.SheetName))
                                        .ToArray();
                    if (validRules.Length > 0)
                        ruleString = validRules.Aggregate((s1, s2) => $"{s1};{s2}");

                    var export_rule =
                        dc
                        .operator_export_rule
                        .FirstOrDefault(r => r.operator_id == op.Id);

                    if (export_rule != null)
                    {
                        if (export_rule.export_rules != ruleString)
                        {
                            export_rule.export_rules = ruleString;
                            dc.SaveChanges();
                        }
                    } else
                    {
                        dc.operator_export_rule.Add(
                            new operator_export_rule() { operator_id = (int)op.Id, export_rules = ruleString }
                            );
                        dc.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                wasException = true;
                Helpers.Old.Log.Add(logSesson, ex.GetExceptionText());
                throw ex;
            }
            finally
            {
                Helpers.Old.Log.SessionEnd(logSesson, wasException);
            }
        }

        public ExelConvertionRule[] GetRulesByOperator(Operator op)
        {
            return GetRulesByOperator(new int[] { (int)op.Id });
        }

        public ExelConvertionRule[] GetRulesByOperator(int[] ids)
        {
            bool wasException = false;
            var logSesson = Helpers.Old.Log.SessionStart("DataAccess.GetRulesByOperator()", true);
            try
            {
                using (var dc = exelconverterEntities2.New())
                {
                    ids = ids == null ? new int[] { } : ids;
                    bool isIdsEmpty = ids.Count() == 0;

                    ExelConvertionRule[] result =
                        dc
                        .convertion_rules
                        .Where(cr => isIdsEmpty || ids.Contains(cr.fk_operator_id))
                        .AsEnumerable()
                        .AsParallel()
                        .Select(cr => GetRuleFromRow(cr))
                        .ToArray();
                    return result;
                }
            }
            catch (Exception ex)
            {
                wasException = true;
                Helpers.Old.Log.Add(logSesson, ex);
                throw ex;
            }
            finally
            {
                Helpers.Old.Log.SessionEnd(logSesson, wasException);
            }
        }

        public ExelConvertionRule[] GetRules(int[] ids = null)
        {
            ExelConvertionRule[] result = null;
            bool wasException = false;

            var logSesson = Helpers.Old.Log.SessionStart("DataAccess.GetRules()", true);
            try
            { 
                using (var dc = exelconverterEntities2.New())
                {
                    ids = ids == null ? new int[] { } : ids;
                    bool isIdsEmpty = ids.Count() == 0;

                    result =
                        dc
                        .convertion_rules
                        .Where(cr => isIdsEmpty || ids.Contains(cr.id))
                        .AsEnumerable()
                        .Select(cr => GetRuleFromRow(cr))
                        .ToArray();
                }
            }
            catch(Exception ex)
            {
                wasException = true;
                Helpers.Old.Log.Add(logSesson, ex);
                throw ex;
            }
            finally
            {
                Helpers.Old.Log.SessionEnd(logSesson, wasException);
            }
            return result;
        }

        public ExelConvertionRule GetRule(int id)
        {
            return GetRules(new int[] { id }).FirstOrDefault();
        }

        private ExelConvertionRule GetRuleFromRow(convertion_rules rl)
        {
            ExelConvertionRule rule = null;

            if (rule == null && !string.IsNullOrWhiteSpace(rl.convertion_rule))
                rule = ExelConvertionRule.DeserializeFromB64String(rl.convertion_rule);

            if (rule == null && rl.convertion_rule_image != null && rl.convertion_rule_image.Length > 0)
                rule = ExelConvertionRule.DeserializeFromBytes(rl.convertion_rule_image);

            if (rule == null && rl.convertion_rule_image_cprs != null && rl.convertion_rule_image_cprs.Length > 0)
                rule = ExelConvertionRule.DeserializeFromCompressedBytes(rl.convertion_rule_image_cprs);

            if (rule == null)
                rule = new ExelConvertionRule() { Name = ExelConvertionRule.DefaultName };

            rule.Id = rl.id;
            rule.FkOperatorId = rl.fk_operator_id;

            return rule;
        }

        private convertion_rules SetRuleToRow(convertion_rules rl, ExelConvertionRule rule)
        {
            if (rl == null)
                rl = new convertion_rules();

            rl.convertion_rule = string.Empty;// do not save old rule // serializedRule;
            rl.convertion_rule_image = null;// do not save old rule //rule.SerializeToBytes();
            rl.convertion_rule_image_cprs = rule.SerializeToCompressedBytes();
            rl.fk_operator_id = rule.FkOperatorId;

            return rl;
        }

        public void UpdateOperatorRules(Converter.ExelConvertionRule[] rules)
        {
            if (rules != null && rules.Length > 0)
                using (var dc = exelconverterEntities2.New())
                {
                    //var dc = exelconverterEntities2.Default;

                    var rulesIds = rules.Select(i => i.Id).ToArray();
                    bool needSave = false;
                    foreach (var rule in rules)
                    {
                        var rl = dc.convertion_rules.Where(r => r.id == rule.Id).FirstOrDefault();
                        if (rl != null)
                        {
                            //ExelConvertionRule oldRule = GetRuleFromRow(rl);

                            //var serializedRule = string.Empty;
                            //if (!checkAfterUpdate || oldRule.Serialize().Trim() != (serializedRule = rule.Serialize()).Trim() && oldRule.SerializeXML().Trim() != rule.SerializeXML().Trim())
                            //{
                                needSave = true;
                                SetRuleToRow(rl, rule);
                            //}
                        }
                    }
                    if (needSave)
                        dc.SaveChanges();
                }
        }

        public void AddOperatorRule(ExelConvertionRule rule)
        {
            AddOperatorRules(new ExelConvertionRule[] { rule });
        }

        public void RemoveOperatorRule(ExelConvertionRule rule)
        {
            RemoveOpertaorRule(new ExelConvertionRule[] { rule });
        }

        public void RemoveOpertaorRule(ExelConvertionRule[] rules)
        {
            if (rules != null && rules.Length > 0)
                RemoveOpertaorRule(rules.Select(r => r.Id).ToArray());
        }

        public void RemoveOpertaorRule(int[] rulesIds)
        {
            if (rulesIds != null && rulesIds.Length > 0)
                using (var dc = exelconverterEntities2.New())
                {
                    var rl = dc.convertion_rules.Where(r => rulesIds.Contains(r.id)).ToArray();
                    foreach (var r in rl)
                        dc.convertion_rules.Remove(r);
                    
                    dc.SaveChanges();
                }
        }

        public void AddOperatorRules(ExelConvertionRule[] rules)
        {
            if (rules != null && rules.FirstOrDefault() != null)
                try
                { 
                    using (var dc = exelconverterEntities2.New())
                    {
                        convertion_rules[] convRules =
                            rules
                            .Select(r => SetRuleToRow(null, r))
                            .ToArray();

                        foreach (var rule in convRules)
                            dc.convertion_rules.Add(rule);
                   
                        dc.SaveChanges();

                        for (int i = 0; i < convRules.Length; i++)
                            rules[i].Id = convRules[i].id;
                    }
                }
                catch(DbEntityValidationException ex)
                {
                    StringBuilder sb = new StringBuilder();

                    foreach (var failure in ex.EntityValidationErrors)
                    {
                        sb.AppendFormat("{0} failed validation\n", failure.Entry.Entity.GetType());
                        foreach (var error in failure.ValidationErrors)
                        {
                            sb.AppendFormat("- {0} : {1}", error.PropertyName, error.ErrorMessage);
                            sb.AppendLine();
                        }
                    }

                    throw new DbEntityValidationException(
                        "Entity Validation Failed - errors follow:\n" +
                        sb.ToString(), ex
                    ); // Add the original exception as the innerException
                }
        }

        public Dictionary<Operator, User> GetOperatorLockers(Operator operatorFilter = null)
        {
            var result = new Dictionary<Operator, User>();
            if (IsNoLocking)
                return result;

            var logSession = Helpers.Old.Log.SessionStart(string.Format("DataAccess.GetOperatorLockers(operatorFilter:'{0}')", operatorFilter == null ? "null" : operatorFilter.Id.ToString()), true);
            bool wasException = false;
            try
            {
                DateTime startDateTime = GetServerDateTime();

                long opId = operatorFilter == null ? -1 : operatorFilter.Id;

                using (var dbApp = exelconverterEntities2.New())
                using (var dbMain = alphaEntities.New())
                {
                    var curLocks =
                        (
                            from l in dbApp.locks.Where(
                                l => (l.locked_to > startDateTime)
                                     && (opId == -1 || l.id_company == opId)
                                ).ToList()
                            join user in dbMain.users on l.id_user equals user.id
                            join op in dbMain.companies on l.id_company equals op.id
                            select new
                            {
                                User = new User()
                                {
                                    Id = user.id,
                                    Login = user.login,
                                    Name = user.name,
                                    Surname = user.surname,
                                    Patronymic = user.patronymic
                                }
                                ,
                                Operator = new Operator()
                                    {
                                        Id = op.id,
                                        Name = op.name
                                    }
                            }
                        ).ToList();

                    foreach (var l in curLocks)
                        result.Add(l.Operator, l.User);
                }
            }
            catch(Exception ex)
            {
                wasException = true;
                Helpers.Old.Log.Add(logSession, ex.GetExceptionText());
            }
            finally
            {
                Helpers.Old.Log.SessionEnd(logSession, wasException);
            }
            return result;
        }

        public User GetOperatorLocker(Operator op)
        {
            var res = GetOperatorLockers(op);
            return res.Select(o => o.Value).FirstOrDefault();
        }

        private DateTime GetServerDateTime()
        {
            DateTime curDate = DateTime.UtcNow; //should get it from server

            using (var conn = new MySql.Data.MySqlClient.MySqlConnection(exelconverterEntities2.ProviderConnectionString))
            using (MySql.Data.MySqlClient.MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand("SELECT now();", conn))
            {
                conn.Open();
                var rd = cmd.ExecuteReader();
                rd.Read();
                curDate = rd.GetDateTime(0);
            }

            return curDate;
        }

        public TimeSpan GetOperatorLockerTimeout()
        {
            return TimeSpan.FromSeconds(30);
        }

        private DateTime GetDateTimeToLock(DateTime? curDateTime = null)
        {
            return (curDateTime ?? GetServerDateTime()) + GetOperatorLockerTimeout();
        }

        public bool SetOperatorLocker(Operator op, User user, bool _lock)
        {
            if (IsNoLocking)
                return true;

            bool wasException = false;
            var logSession = Helpers.Old.Log.SessionStart("DataAccess.SetOperatorLocker()", true);
            bool result = false;

            try
            {
                using (var dc = exelconverterEntities2.New())
                {
                    if (_lock)
                    {
                        DateTime curDate = GetServerDateTime();

                        //Берем исключительно последнюю блокировку для данного оператора
                        var currentLock = dc.locks.Where(l => l.id_company == op.Id).OrderByDescending(l => l.locked_to).FirstOrDefault();
                        if (currentLock != null)
                        {
                            //Если это блокировка текущего пользователя или если это старая блокировка (пофиг кем сделанная)
                            if (currentLock.id_user == user.Id || currentLock.locked_to < curDate)
                            {
                                currentLock.locked_to = GetDateTimeToLock(curDate);
                                currentLock.id_user = (int)user.Id;
                                dc.SaveChanges();
                                result = true;
                            }
                        } else
                        {
                            dc.locks.Add(new locks() { id_company = (int)op.Id, id_user = (int)user.Id, locked_to = GetDateTimeToLock(curDate) });
                            dc.SaveChanges();
                            result = true;
                        }
                    } else
                    {
                        foreach (var currentLock in dc.locks.Where(l => l.id_company == op.Id && l.id_user == user.Id).ToArray())
                            dc.locks.Remove(currentLock);
                        
                        dc.SaveChanges();
                        result = true;
                    }
                }
            }
            catch(Exception ex)
            {
                wasException = true;
                Helpers.Old.Log.Add(logSession, ex.GetExceptionText());
            }
            finally
            {
                Helpers.Old.Log.SessionEnd(logSession, wasException);
            }
            return result;
        }

        #endregion
        #region Web part

        public void AddFillRectangle(FillArea rect)
        {
            if (!FillRectExists(rect))
            {
                HttpDataClient.Default.AddFillRectangle(rect);
                //HttpDataAccess.AddFillRectangle(rect);
            }
        }

        public void RemoveFillRectangle(int id)
        {
            HttpDataClient.Default.RemoveFillRectangle(id);
            //HttpDataAccess.RemoveFillRectangle(id);
        }

        #endregion
    }
}
