using BrendanGrant.Helpers.FileAssociation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;


namespace Server.Helpers;
public static class ExecutionHelper
{
    public static void Execute(string filePath)
    {
        try
        {
            var data = File.ReadAllBytes(filePath);
            if (IsFileMP3(data))
            {
                var association = new FileAssociationInfo(".mp3");
                var progInfo = new ProgramAssociationInfo(association.ProgID);
                Process.Start(progInfo.Verbs[0].Command, filePath);
            }
        }
        catch { }
    }

    private static bool IsFileMP3(byte[] data)
    {
        // Check the first few bytes of the file to identify the MP3 file signature
        if (data[0] == 0x49 && data[1] == 0x44 && data[2] == 0x33)
        {
            return true;
        }
        return false;
    }
}
