package io.xol.chunkstories.core.net.packets;

import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.io.IOException;

import io.xol.chunkstories.api.client.net.ClientPacketsProcessor;
import io.xol.chunkstories.api.exceptions.PacketProcessingException;
import org.joml.Vector3d;
import io.xol.chunkstories.api.net.PacketDestinator;
import io.xol.chunkstories.api.net.PacketSender;
import io.xol.chunkstories.api.net.PacketSynchPrepared;
import io.xol.chunkstories.api.net.PacketsProcessor;
import io.xol.chunkstories.core.util.WorldEffects;

//(c) 2015-2017 XolioWare Interactive
//http://chunkstories.xyz
//http://xol.io

public class PacketExplosionEffect extends PacketSynchPrepared
{
	Vector3d center;
	double radius;
	double debrisSpeed;
	float f;

	public PacketExplosionEffect()
	{
		
	}
	
	public PacketExplosionEffect(Vector3d center, double radius, double debrisSpeed, float f)
	{
		super();
		this.center = center;
		this.radius = radius;
		this.debrisSpeed = debrisSpeed;
		this.f = f;
	}

	@Override
	public void sendIntoBuffer(PacketDestinator destinator, DataOutputStream out) throws IOException
	{
		out.writeDouble(center.x());
		out.writeDouble(center.y());
		out.writeDouble(center.z());

		out.writeDouble(radius);
		out.writeDouble(debrisSpeed);
		
		out.writeFloat(f);
	}

	@Override
	public void process(PacketSender sender, DataInputStream in, PacketsProcessor processor) throws IOException, PacketProcessingException
	{
		center = new Vector3d(in.readDouble(), in.readDouble(), in.readDouble());
		radius = in.readDouble();
		debrisSpeed = in.readDouble();
		f = in.readFloat();
		
		if(processor instanceof ClientPacketsProcessor)
		{
			ClientPacketsProcessor cpp = (ClientPacketsProcessor)processor;
			WorldEffects.createFireballFx(cpp.getWorld(), center, radius, debrisSpeed, f);
		}
	}
	
}
