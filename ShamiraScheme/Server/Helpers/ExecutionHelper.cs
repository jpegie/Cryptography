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
            var association = new FileAssociationInfo(".mp3");
            var progInfo = new ProgramAssociationInfo(association.ProgID);
            Process.Start(progInfo.Verbs[0].Command, filePath);
        }
        catch { }
    }
}
