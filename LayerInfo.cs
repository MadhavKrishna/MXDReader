/*
 * Erstellt mit SharpDevelop.
 * Benutzer: mri0
 * Datum: 09.10.2009
 * Zeit: 10:32
 * 
 * Sie k�nnen diese Vorlage unter Extras > Optionen > Codeerstellung > Standardheader �ndern.
 */

using System;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;

namespace MXDReader
{
	/// <summary>
	/// Description of LayerInfo.
	/// </summary>
	public class LayerInfo
	{
		private string mxdname = "";
		private string username = "";
		private string server = "";
		private string instance = "";
		private string version = "";	
		private string name = "";
		private string owner = "";
		private string tablename = "";
		private string type = "";
		private string defquery = "";
		private string joininfo = "";
		private string minscale = "";
		private string maxscale = "";
		private string symbfields = "";
		
		public LayerInfo(string mxd)
		{
			/* Diese Eigenschaften sind f�r jeden Layer identisch */
			mxdname = mxd;
		}
		
		private void fillLayerProps(ILayer lyr)
		{
			name = lyr.Name;
			minscale = lyr.MinimumScale.ToString();
			maxscale = lyr.MaximumScale.ToString();
		}
		
		private void fillDatalayerProps(ILayer lyr)
		{
			IDataLayer dlyr = lyr as IDataLayer;
			IDatasetName dataName = dlyr.DataSourceName as IDatasetName;
			// nur f�r SDE-Layer werden die Connection-Infos ausgegeben
			// bei �brigen Layern wird nur der Pfad ausgegeben
			if (dataName.WorkspaceName.Type == esriWorkspaceType.esriRemoteDatabaseWorkspace)
			{
				IPropertySet props = dataName.WorkspaceName.ConnectionProperties;
				username = props.GetProperty("user").ToString();
				server = props.GetProperty("server").ToString();
				instance = props.GetProperty("instance").ToString();
				version = props.GetProperty("version").ToString();
				string[] s = dataName.Name.Split('.');
				owner = s[0];
				tablename = s[1];
			} else {
				tablename = dataName.Name;
				server = dataName.WorkspaceName.PathName;
				type = "kein SDE-Layer";
			}
		}
		
		private void fillDefQueryProps(IFeatureLayer flyr)
		{
			IFeatureLayerDefinition flyrdef = flyr as IFeatureLayerDefinition;
			if (flyrdef.DefinitionExpression != "")
			{
				defquery = flyrdef.DefinitionExpression;
			}
		}
		
		public void processFeatureLayer(ILayer lyr)
		{
			type = "Vektor";
			fillLayerProps(lyr);
			fillDatalayerProps(lyr);
			IFeatureLayer flyr = lyr as IFeatureLayer;
			fillDefQueryProps(flyr);
			processJoins(flyr);
			
			IGeoFeatureLayer gflyr = flyr as IGeoFeatureLayer;
			if (gflyr.DisplayAnnotation == true)
			{
				// es wird gelabelt
			}
			IFeatureRenderer rend = gflyr.Renderer;
			if (rend is IUniqueValueRenderer)
			{
				string felder = "";
				IUniqueValueRenderer u = rend as IUniqueValueRenderer;
				for (int i = 0; i < u.FieldCount; i++) 
				{
					felder = felder + u.get_Field(i) + "/";
				}
				symbfields = felder.TrimEnd('/') + " (UniqueValueRenderer)";
			} else if (rend is IProportionalSymbolRenderer)
			{
				IProportionalSymbolRenderer prop = rend as IProportionalSymbolRenderer;
				symbfields = prop.Field + " (ProportionalSymbolRenderer)";
			} else if (rend is IClassBreaksRenderer)
			{
				IClassBreaksRenderer cl = rend as IClassBreaksRenderer;
				symbfields = cl.Field + " (ClassBreaksRenderer)";;
			} else if (rend is ISimpleRenderer)
			{
				symbfields = "kein Feld (SimpleRenderer)";
			} else
			{
				symbfields = "unbekannter Renderer";
			}
		}
		
		private void processJoins(IFeatureLayer flyr)
		{
			IDisplayTable dispTbl = flyr as IDisplayTable;
			ITable tbl = dispTbl.DisplayTable;
			IRelQueryTable rqt;
			ITable destTable;
			IDataset dataset;
			string destName;
			string destServer;
			string destInstance;
			string destUser;
			string res = "";
			string joinType;
			// Holt iterativ alle Joins!
			while (tbl is IRelQueryTable)
			{
				rqt = (IRelQueryTable)tbl;
				IRelQueryTableInfo rqtInfo = (IRelQueryTableInfo)rqt;
				IRelationshipClass relClass = rqt.RelationshipClass;
				destTable = rqt.DestinationTable;
				if (rqtInfo.JoinType == esriJoinType.esriLeftInnerJoin)
				{
					joinType = "esriLeftInnerJoin";
				} else
				{
					joinType = "esriLeftOuterJoin";
				}
				dataset = (IDataset)destTable;
				destName = dataset.Name;
				destServer = dataset.Workspace.ConnectionProperties.GetProperty("server").ToString();
				destInstance = dataset.Workspace.ConnectionProperties.GetProperty("instance").ToString();
				destUser = dataset.Workspace.ConnectionProperties.GetProperty("user").ToString();
				res = res + "(" + destName + "/" + destServer + "/" + destInstance + "/" + destUser + "/" + relClass.OriginPrimaryKey + "/" + relClass.OriginForeignKey + "/" + joinType + ")";
				tbl = rqt.SourceTable;
			}
			joininfo = res;
		}

		public void processRasterCatalogLayer(ILayer lyr)
		{
			type = "RasterKatalog";
			fillLayerProps(lyr);
			fillDatalayerProps(lyr);
			IFeatureLayer flyr = lyr as IFeatureLayer;
			fillDefQueryProps(flyr);
		}
		
		public void processAnnotationLayer(ILayer lyr)
		{
			type = "Annotation";
			fillLayerProps(lyr);
			fillDatalayerProps(lyr);
			IFeatureLayer flyr = lyr as IFeatureLayer;
			fillDefQueryProps(flyr);
		}
		
		public void processAnnotationSubLayer(ILayer lyr)
		{
			type = "AnnotationClass";
			fillLayerProps(lyr);
			// bei AnnotationSubLayern hat nur der Parent-AnnotationLayer die
			// gew�nschten Informationen.
			IAnnotationSublayer sub = lyr as IAnnotationSublayer;
			ILayer parentLayer = sub.Parent as ILayer;
			fillDatalayerProps(parentLayer);
			IFeatureLayer flyr = parentLayer as IFeatureLayer;
			fillDefQueryProps(flyr);
		}

		public void processRasterLayer(ILayer lyr)
		{
			type = "Raster";
			fillLayerProps(lyr);
			fillDatalayerProps(lyr);
		}
		
		public void processGroupLayer(ILayer lyr)
		{
			type = "GroupLayer";
			fillLayerProps(lyr);			
		}
		
		public void processOtherLayer(ILayer lyr)
		{
			type = "anderer Layer";
			fillLayerProps(lyr);	
		}
		
		public string writeCSV()
		{
			string output = mxdname + ";" + name + ";" + type + ";" + owner + ";" + tablename + ";" + server + ";" + instance + ";" + username.ToUpper() + ";" + version + ";" + minscale + ";" + maxscale + ";" + defquery + ";" + joininfo + ";" + symbfields;
			return output;
		}
	}
}
