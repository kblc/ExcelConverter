using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExelConverter.Core.DataObjects;
using ExelConverter.Core.Converter;
using ExcelConverter.Parser;

namespace ExelConverter.Core.DataAccess
{
    public interface IDataAccess
    {
        DataObjects.Type[] GetTypes();
        Size[] GetSizes();
        Size[] GetSizesByType(DataObjects.Type type);
        City[] GetCities();
        Region[] GetRegions();
        Region[] GetRegionsByCity(City city);
        string[] GetSidesNames();
        string[] GetLights();

        /// <summary>
        /// Returns user list in database
        /// </summary>
        /// <returns>User list</returns>
        User[] GetUsers(string[] logins = null);
        /// <summary>
        /// Returns user by his login
        /// </summary>
        /// <param name="login">User login</param>
        /// <returns>User with this login</returns>
        User GetUser(string login);

        /// <summary>
        /// Return all locked operators
        /// </summary>
        /// <returns>Return dictonary with all locked operators</returns>
        Dictionary<Operator, User> GetOperatorLockers(Operator operatorFilter = null);
        /// <summary>
        /// Use this to known who lock this operator
        /// </summary>
        /// <param name="op">Operator</param>
        /// <returns>User, who lock this operator</returns>
        User GetOperatorLocker(Operator op);
        /// <summary>
        /// Try to lock operator
        /// </summary>
        /// <param name="op">Operator for lock/unlock</param>
        /// <param name="user">User as locker. Should by <b>NULL</b> if unlock operator</param>
        /// <returns>Lock result. If <b>false</b>, than this operator is currently locked by another user. Use <b>GetOperatorLocker</b> for know who</returns>
        bool SetOperatorLocker(Operator op, User user, bool _lock);

        Parser[] ParsersGet(Guid[] ids = null);
        Guid[] ParsersRemove(Parser[] parsers);
        Guid[] ParsersRemove(Guid[] ids);
        Guid[] ParsersInsert(Parser[] parsers, bool needSave = true);
        Guid[] ParsersUpdate(Parser[] parsers, bool needSave = true);

        Operator[] GetOperators(int[] ids = null);

        Operator GetOperator(int id);

        ExelConvertionRule[] GetRulesByOperator(Operator op);

        ExelConvertionRule[] GetRules(int[] ids = null);

        ExelConvertionRule GetRule(int id);

        void UpdateOperatorRules(ExelConvertionRule[] rules);

        void AddOperatorRule(ExelConvertionRule rule);

        void AddOperatorRules(ExelConvertionRule[] rules);

        void RemoveOperatorRule(ExelConvertionRule rule);

        void RemoveOpertaorRule(ExelConvertionRule[] rules);

        void RemoveOpertaorRule(int[] rules);

        void AddFillRectangle(FillArea rect);

        void RemoveFillRectangle(int id);

        void UpdateFillRectangle(FillArea area);

        FillArea[] GetFillRects(int operatorId = -1, string type = null);

        bool FillRectExists(FillArea area);

        bool[] FillRectExists(FillArea[] area);

        SheetRulePair[] GetExportRulesIdByOperator(Operator op, IQueryable<ExelConverter.Core.ExelDataReader.ExelSheet> existedSheets);

        void SetExportedRulesForOperator(Operator SelectedOperator, SheetRulePair[] exportRules);
    }
}
