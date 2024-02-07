﻿using NUnit.Framework;
using Sharp7.Rx.Interfaces;
using Shouldly;

namespace Sharp7.Rx.Tests.S7ValueConverterTests;

[TestFixture]
public class WriteToBuffer
{
    static readonly IS7VariableNameParser parser = new S7VariableNameParser();

    [TestCase(true, "DB0.DBx0.0", new byte[] {0x01})]
    [TestCase(false, "DB0.DBx0.0", new byte[] {0x00})]
    [TestCase(true, "DB0.DBx0.4", new byte[] {0x10})]
    [TestCase(false, "DB0.DBx0.4", new byte[] {0})]
    [TestCase((byte) 18, "DB0.DBB0", new byte[] {0x12})]
    [TestCase((short) 4660, "DB0.INT0", new byte[] {0x12, 0x34})]
    [TestCase((short) -3532, "DB0.INT0", new byte[] {0xF2, 0x34})]
    [TestCase(-3532, "DB0.INT0", new byte[] {0xF2, 0x34})]
    [TestCase(305419879, "DB0.DINT0", new byte[] {0x12, 0x34, 0x56, 0x67})]
    [TestCase(-231451033, "DB0.DINT0", new byte[] {0xF2, 0x34, 0x56, 0x67})]
    [TestCase(1311768394163015151L, "DB0.dul0", new byte[] {0x12, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF})]
    [TestCase(-994074615050678801L, "DB0.dul0", new byte[] {0xF2, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF})]
    [TestCase(1311768394163015151uL, "DB0.dul0", new byte[] {0x12, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF})]
    [TestCase(17452669458658872815uL, "DB0.dul0", new byte[] {0xF2, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF})]
    [TestCase(new byte[] {0x12, 0x34, 0x56, 0x67}, "DB0.DBB0.4", new byte[] {0x12, 0x34, 0x56, 0x67})]
    [TestCase(0.25f, "DB0.D0", new byte[] {0x3E, 0x80, 0x00, 0x00})]
    [TestCase("ABCD", "DB0.string0.4", new byte[] {0x04, 0x04, 0x41, 0x42, 0x43, 0x44})]
    [TestCase("ABCD", "DB0.string0.8", new byte[] {0x08, 0x04, 0x41, 0x42, 0x43, 0x44, 0x00, 0x00, 0x00, 0x00})] 
    [TestCase("ABCD", "DB0.string0.2", new byte[] {0x02, 0x02, 0x41, 0x42})] 
    [TestCase("ABCD", "DB0.DBB0.4", new byte[] {0x41, 0x42, 0x43, 0x44})]
    public void Write<T>(T input, string address, byte[] expected)
    {
        //Arrange
        var variableAddress = parser.Parse(address);
        var buffer = new byte[variableAddress.BufferLength];

        //Act
        S7ValueConverter.WriteToBuffer(buffer, input, variableAddress);

        //Assert
        buffer.ShouldBe(expected);
    }

    [TestCase((char) 18, "DB0.DBB0")]
    [TestCase(0.25, "DB0.D0")]
    public void Invalid<T>(T input, string address)
    {
        //Arrange
        var variableAddress = parser.Parse(address);
        var buffer = new byte[variableAddress.BufferLength];

        //Act
        Should.Throw<InvalidOperationException>(() => S7ValueConverter.WriteToBuffer<T>(buffer, input, variableAddress));
    }
}
