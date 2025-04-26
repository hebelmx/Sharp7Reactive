using Sharp.Rx.Extensions;
using Sharp.Rx;


using Shouldly;
using System;
using System.Linq;
using Sharp7.Rx;
using Sharp7.Rx.Enums;
using Xunit;
using DbType = Sharp7.Rx.Enums.DbType;

namespace Sharp.Rx.Extensions.Tests;
 public class PlcAccessTrackerTests
{
    [Fact]
    public void Should_Add_New_DbAccessInfo_When_Tracking_FirstVariable()
    {
        var tracker = new PlcAccessTracker();
        var address = new VariableAddress(Operand.Db, 1, DbType.Byte, 0, 4);

        tracker.TrackAccess("DB1.MyVar", address, typeof(int));

        var dbs = tracker.GetAllAccesses().ToList();
        dbs.Count.ShouldBe(1);
        dbs[0].DbNo.ShouldBe(1);
        dbs[0].Variables.Count().ShouldBe(1);
    }

    [Fact]
    public void Should_Not_Duplicate_Variables_With_Same_Signature()
    {
        var tracker = new PlcAccessTracker();
        var address = new VariableAddress(Operand.Db, 1, DbType.Byte, 0, 4);

        tracker.TrackAccess("DB1.MyVar", address, typeof(int));
        tracker.TrackAccess("DB1.MyVar", address, typeof(int)); // same signature

        var db = tracker.GetAllAccesses().First();
        db.Variables.Count().ShouldBe(1);
    }

    [Fact]
    public void Should_Correctly_Track_Max_Offset_Read()
    {
        var tracker = new PlcAccessTracker();
        tracker.TrackAccess("Var1", new VariableAddress(Operand.Db, 2, DbType.Byte, 0, 4), typeof(int));
        tracker.TrackAccess("Var2", new VariableAddress(Operand.Db, 2, DbType.Byte, 10, 10), typeof(byte[]));

        var db = tracker.GetAllAccesses().First(d => d.DbNo == 2);
        db.MaxOffsetRead.ShouldBe(20); // Start 10 + Length 10
    }

    [Fact]
    public void Should_Track_Multiple_DbNos()
    {
        var tracker = new PlcAccessTracker();
        tracker.TrackAccess("VarA", new VariableAddress(Operand.Db, 1, DbType.Byte, 0, 4), typeof(int));
        tracker.TrackAccess("VarB", new VariableAddress(Operand.Db, 3, DbType.Int, 2, 2), typeof(short));

        var dbs = tracker.GetAllAccesses().ToList();
        dbs.Count.ShouldBe(2);
        dbs.Select(d => d.DbNo).ShouldBe([1, 3]);
    }

    [Fact]
    public void Should_Track_String_With_Correct_BufferLength()
    {
        var tracker = new PlcAccessTracker();
        var address = new VariableAddress(Operand.Db, 4, DbType.String, 100, 20); // Length = 20, +2 for string overhead

        tracker.TrackAccess("DB4.StringVar", address, typeof(string));

        var db = tracker.GetAllAccesses().Single(d => d.DbNo == 4);
        var tracked = db.Variables.Single();
        tracked.OffsetEnd.ShouldBe(122); // 100 + (20 + 2)
    }





    [Fact]
    public void Should_Track_Bit_Variable_Correctly()
    {
        var tracker = new PlcAccessTracker();
        var address = new VariableAddress(Operand.Db, 5, DbType.Bit, 10, 1, Bit: 3);

        tracker.TrackAccess("DB5.BitVar", address, typeof(bool));

        var db = tracker.GetAllAccesses().Single(d => d.DbNo == 5);
        var tracked = db.Variables.Single();
        tracked.Start.ShouldBe(10);
        tracked.Length.ShouldBe(1);
    }

   
  

    [Fact]
    public void Should_Track_Single_Float_Variable()
    {
        var tracker = new PlcAccessTracker();
        var address = new VariableAddress(Operand.Db, 6, DbType.Single, 30, 4); // Float (Single) usually occupies 4 bytes

        tracker.TrackAccess("DB6.FloatVar", address, typeof(float));

        var db = tracker.GetAllAccesses().Single(d => d.DbNo == 6);
        db.MaxOffsetRead.ShouldBe(34); // 30 + 4
    }


    [Fact]
    public void Should_Track_DInt_Correctly()
    {
        var tracker = new PlcAccessTracker();
        var address = new VariableAddress(Operand.Db, 7, DbType.DInt, 50, 4); // DInt = 32-bit signed int

        tracker.TrackAccess("DB7.DIntVar", address, typeof(int));

        var db = tracker.GetAllAccesses().Single(d => d.DbNo == 7);
        db.MaxOffsetRead.ShouldBe(54);
    }

    [Fact]
    public void Should_Track_MixedVariablesAcrossMultipleDbs_WithS7StyleNames()
    {
        var tracker = new PlcAccessTracker();

        // DB8 variables (String, Bit, Float)
        tracker.TrackAccess("DB8.DBB0", new VariableAddress(Operand.Db, 8, DbType.String, 0, 10), typeof(string)); // ASCII string
        tracker.TrackAccess("DB8.DBX10.1", new VariableAddress(Operand.Db, 8, DbType.Bit, 10, 1, Bit: 1), typeof(bool)); // Bit at DBX10.1
        tracker.TrackAccess("DB8.DBD12", new VariableAddress(Operand.Db, 8, DbType.Single, 12, 4), typeof(float)); // Float at DBD12

        // DB9 variables (DInt, Byte array, WString)
        tracker.TrackAccess("DB9.DBD5", new VariableAddress(Operand.Db, 9, DbType.DInt, 5, 4), typeof(int)); // DInt at DBD5
        tracker.TrackAccess("DB9.DBB20", new VariableAddress(Operand.Db, 9, DbType.Byte, 20, 5), typeof(byte[])); // Byte array at DBB20
        tracker.TrackAccess("DB9.DBS30.10", new VariableAddress(Operand.Db, 9, DbType.WString, 30, 10), typeof(string)); // WString at DBS30.10

        var allAccesses = tracker.GetAllAccesses().ToList();

        // Assert DB count
        allAccesses.Count.ShouldBe(2);

        // DB8 assertions
        var db8 = allAccesses.Single(d => d.DbNo == 8);
        db8.Variables.Count().ShouldBe(3);
        db8.MaxOffsetRead.ShouldBe(16); // 12 + 4 (float)

        // DB9 assertions
        var db9 = allAccesses.Single(d => d.DbNo == 9);
        db9.Variables.Count().ShouldBe(3);
        db9.MaxOffsetRead.ShouldBe(54); // 30 + (10*2 + 4) = 54 for WString
    }


    [Fact]
    public void Should_Track_Realistic_Db257_And_Db256_Accesses()
    {
        var tracker = new PlcAccessTracker();

        // DB257 Ints (W0 to W16)
        ushort[] intStarts = { 0, 2, 12, 14, 16 };
        foreach (var start in intStarts)
            tracker.TrackAccess($"DB257.W{start}", new VariableAddress(Operand.Db, 257, DbType.Int, start, 2), typeof(short));

        // DB257 DInts (D60-D96)
        ushort[] dintStarts = { 60, 68, 76, 80, 84, 88, 92, 96 };
        foreach (var start in dintStarts)
            tracker.TrackAccess($"DB257.D{start}", new VariableAddress(Operand.Db, 257, DbType.DInt, start, 4), typeof(int));

        // DB257 Strings
        tracker.TrackAccess("DB257.S100.32", new VariableAddress(Operand.Db, 257, DbType.String, 100, 32), typeof(string));
        tracker.TrackAccess("DB257.S150.32", new VariableAddress(Operand.Db, 257, DbType.String, 150, 32), typeof(string));

        // DB257 Bits
        tracker.TrackAccess("DB257.X10.2", new VariableAddress(Operand.Db, 257, DbType.Bit, 10, 1, 2), typeof(bool));
        tracker.TrackAccess("DB257.X10.3", new VariableAddress(Operand.Db, 257, DbType.Bit, 10, 1, 3), typeof(bool));
        tracker.TrackAccess("DB257.X10.4", new VariableAddress(Operand.Db, 257, DbType.Bit, 10, 1, 4), typeof(bool));

        // DB256 DINTs
        tracker.TrackAccess("DB256.DINT48", new VariableAddress(Operand.Db, 256, DbType.DInt, 48, 4), typeof(int));
        tracker.TrackAccess("DB256.DINT52", new VariableAddress(Operand.Db, 256, DbType.DInt, 52, 4), typeof(int));

        // Asserts
        var db257 = tracker.GetAllAccesses().First(d => d.DbNo == 257);
        db257.Variables.Count().ShouldBe(18); // 5 Ints + 8 DInts + 2 Strings + 3 Bits
        db257.MaxOffsetRead.ShouldBe(184);    // 150 + (32 + 2)

        var db256 = tracker.GetAllAccesses().First(d => d.DbNo == 256);
        db256.Variables.Count().ShouldBe(2);
        db256.MaxOffsetRead.ShouldBe(56);     // 52 + 4
    }
    [Fact]
    public void Should_Track_Refined_Db257_And_Db256_Accesses()
    {
        var tracker = new PlcAccessTracker();

        // DB257 Word variables
        tracker.TrackAccess("DB257.W0", new VariableAddress(Operand.Db, 257, DbType.Int, 0, 2), typeof(short));
        tracker.TrackAccess("DB257.W2", new VariableAddress(Operand.Db, 257, DbType.Int, 2, 2), typeof(short));

        // DB257 DInts
        ushort[] db257DIntStarts = { 60, 68, 76, 80, 84, 88, 92, 96 };
        foreach (var start in db257DIntStarts)
            tracker.TrackAccess($"DB257.D{start}", new VariableAddress(Operand.Db, 257, DbType.DInt, start, 4), typeof(int));

        // DB257 Strings
        tracker.TrackAccess("DB257.S100", new VariableAddress(Operand.Db, 257, DbType.String, 100, 32), typeof(string));
        tracker.TrackAccess("DB257.DBS150.32", new VariableAddress(Operand.Db, 257, DbType.String, 150, 32), typeof(string));

        // DB257 Bits
        tracker.TrackAccess("DB257.X10.2", new VariableAddress(Operand.Db, 257, DbType.Bit, 10, 1, 2), typeof(bool));
        tracker.TrackAccess("DB257.X10.3", new VariableAddress(Operand.Db, 257, DbType.Bit, 10, 1, 3), typeof(bool));
        tracker.TrackAccess("DB257.X10.4", new VariableAddress(Operand.Db, 257, DbType.Bit, 10, 1, 4), typeof(bool));

        // DB256 DInts
        tracker.TrackAccess("DB256.DINT48", new VariableAddress(Operand.Db, 256, DbType.DInt, 48, 4), typeof(int));
        tracker.TrackAccess("DB256.DINT52", new VariableAddress(Operand.Db, 256, DbType.DInt, 52, 4), typeof(int));

        // Assertions
        var db257 = tracker.GetAllAccesses().First(d => d.DbNo == 257);
        db257.Variables.Count().ShouldBe(15);
        db257.MaxOffsetRead.ShouldBe(184); // 150 + 32 + 2

        var db256 = tracker.GetAllAccesses().First(d => d.DbNo == 256);
        db256.Variables.Count().ShouldBe(2);
        db256.MaxOffsetRead.ShouldBe(56); // 52 + 4
    }


    [Fact]
    public void Should_Handle_Heavy_Dataset_In_Db257_And_Db256()
    {
        var tracker = new PlcAccessTracker();

        // Core DB257 Ints
        tracker.TrackAccess("DB257.W0", new VariableAddress(Operand.Db, 257, DbType.Int, 0, 2), typeof(short));
        tracker.TrackAccess("DB257.W2", new VariableAddress(Operand.Db, 257, DbType.Int, 2, 2), typeof(short));

        // DINTs (with duplication to validate deduplication)
        ushort[] db257Dints = { 76, 80, 76, 80, 84, 88, 92, 96 };
        foreach (var start in db257Dints)
            tracker.TrackAccess($"DB257.D{start}", new VariableAddress(Operand.Db, 257, DbType.DInt, start, 4), typeof(int));

        // Strings
        tracker.TrackAccess("DB257.S100.32", new VariableAddress(Operand.Db, 257, DbType.String, 100, 32), typeof(string));
        tracker.TrackAccess("DB257.S150.32", new VariableAddress(Operand.Db, 257, DbType.String, 150, 32), typeof(string));

        // Bits
        byte[] bits = { 2, 3, 4, 5 };
        foreach (var b in bits)
            tracker.TrackAccess($"DB257.X10.{b}", new VariableAddress(Operand.Db, 257, DbType.Bit, 10, 1, b), typeof(bool));

        // High-offset DBW word series (Int16s)
        for (ushort offset = 300; offset <= 442; offset += 2)
        {
            tracker.TrackAccess($"DB257.DBW{offset}", new VariableAddress(Operand.Db, 257, DbType.Int, offset, 2), typeof(short));
        }

        // DB256 DInts
        tracker.TrackAccess("DB256.DINT48", new VariableAddress(Operand.Db, 256, DbType.DInt, 48, 4), typeof(int));
        tracker.TrackAccess("DB256.DINT52", new VariableAddress(Operand.Db, 256, DbType.DInt, 52, 4), typeof(int));

        // Assert DB256
        var db256 = tracker.GetAllAccesses().Single(d => d.DbNo == 256);
        db256.Variables.Count().ShouldBe(2);
        db256.MaxOffsetRead.ShouldBe(56);

        // Assert DB257
        var db257 = tracker.GetAllAccesses().Single(d => d.DbNo == 257);
        db257.Variables.Count().ShouldBe(2    // W0, W2
            + 6      // unique DINTs (76, 80, 84, 88, 92, 96)
            + 2      // strings
            + 4      // bits
            + ((442 - 300) / 2 + 1)); // 72 DBWs

        db257.MaxOffsetRead.ShouldBe(444); // Last DBW was 442 (442+2=444), last significant string was at 150 + 34 = 184. Highest is 444
    }

}
