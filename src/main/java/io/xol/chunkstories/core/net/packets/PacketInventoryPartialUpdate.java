package io.xol.chunkstories.core.net.packets;

import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.io.IOException;

import io.xol.chunkstories.api.exceptions.NullItemException;
import io.xol.chunkstories.api.exceptions.PacketProcessingException;
import io.xol.chunkstories.api.exceptions.UndefinedItemTypeException;
import io.xol.chunkstories.api.item.inventory.Inventory;
import io.xol.chunkstories.api.item.inventory.ItemPile;
import io.xol.chunkstories.api.net.PacketDestinator;
import io.xol.chunkstories.api.net.PacketSender;
import io.xol.chunkstories.api.net.PacketSynchPrepared;
import io.xol.chunkstories.api.net.PacketsProcessor;
import io.xol.chunkstories.api.util.ChunkStoriesLogger.LogLevel;
import io.xol.chunkstories.core.item.inventory.InventoryTranslator;

//(c) 2015-2017 XolioWare Interactive
//http://chunkstories.xyz
//http://xol.io

public class PacketInventoryPartialUpdate extends PacketSynchPrepared
{
	private Inventory inventory;
	private int slotx, sloty;
	private ItemPile itemPile;

	public PacketInventoryPartialUpdate()
	{

	}

	public PacketInventoryPartialUpdate(Inventory inventory, int slotx, int sloty, ItemPile newItemPile)
	{
		this.inventory = inventory;
		this.slotx = slotx;
		this.sloty = sloty;
		this.itemPile = newItemPile;
	}

	@Override
	public void sendIntoBuffer(PacketDestinator destinator, DataOutputStream out) throws IOException
	{
		InventoryTranslator.writeInventoryHandle(out, inventory);

		out.writeInt(slotx);
		out.writeInt(sloty);

		if (itemPile == null)
			out.writeInt(0);
		else
		{
			//out.writeInt(itemPile.getItem().getID());
			itemPile.saveItemIntoStream(out);
		}
	}

	@Override
	public void process(PacketSender sender, DataInputStream in, PacketsProcessor processor) throws IOException, PacketProcessingException
	{
		inventory = InventoryTranslator.obtainInventoryHandle(in, processor);

		int slotx = in.readInt();
		int sloty = in.readInt();

		try
		{
			itemPile = ItemPile.obtainItemPileFromStream(processor.getContext().getContent().items(), in);
		}
		catch (NullItemException e)
		{
			//This is fine.
			itemPile = null;
		}
		catch (UndefinedItemTypeException e)
		{
			//This is slightly more problematic.
			processor.getContext().logger().log(e.getMessage(), LogLevel.WARN);
			e.printStackTrace(processor.getContext().logger().getPrintWriter());
			e.printStackTrace();
		}
		
		if (inventory != null)
			inventory.setItemPileAt(slotx, sloty, itemPile);
	}

}
